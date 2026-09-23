// Runs through github-script with trusted default-branch code only.
module.exports = async function cleanup({ github, context, core, env = process.env, fetchImpl = fetch }) {
  const { CLOUDFLARE_API_TOKEN: token, CLOUDFLARE_ACCOUNT_ID: account, PAGES_PROJECT: project } = env;
  if (!token || !account) {
    core.notice('Cloudflare Pages is not configured; skipping preview cleanup.');
    return;
  }
  const dryRun = env.DRY_RUN === 'true';
  const target = context.eventName === 'pull_request_target' ? context.payload.pull_request.number : null;
  const base = `https://api.cloudflare.com/client/v4/accounts/${encodeURIComponent(account)}/pages/projects/${encodeURIComponent(project)}/deployments`;
  async function request(url, method = 'GET') {
    const response = await fetchImpl(url, {
      method,
      headers: { Authorization: `Bearer ${token}` },
      signal: AbortSignal.timeout(30000),
    });
    const body = await response.json();
    if (!response.ok || body.success !== true) {
      // Do not dump responses: deployment metadata can contain environment variables.
      throw new Error(`Cloudflare ${method} failed (HTTP ${response.status}; codes ${(body.errors || []).map(e => e.code).join(',')}).`);
    }
    return body;
  }

  // Snapshot every page before deleting: deleting while paging shifts results.
  // The endpoint rejects per_page above 25 (HTTP 400, code 8000024).
  const deployments = new Map();
  for (let page = 1; ; page++) {
    const body = await request(`${base}?env=preview&per_page=25&page=${page}`);
    if (!Array.isArray(body.result)) throw new Error('Invalid Cloudflare deployment list.');
    if (body.result.length === 0) break;
    for (const deployment of body.result) deployments.set(deployment.id, deployment);
    if (body.result_info?.total_pages && page >= body.result_info.total_pages) break;
  }

  let deleted = 0;
  for (const deployment of deployments.values()) {
    const branch = deployment.deployment_trigger?.metadata?.branch;
    const match = /^pr-([1-9][0-9]*)$/.exec(branch || '');
    if (deployment.environment !== 'preview' || !match) continue;
    const number = Number(match[1]);
    if (!Number.isSafeInteger(number) || (target !== null && number !== target)) continue;

    // Query current state before each deletion: a delayed close event may follow a reopen.
    // Missing PRs/API failures fail closed, rather than treating them as deletable.
    const { data: pr } = await github.rest.pulls.get({ ...context.repo, pull_number: number });
    if (pr.state !== 'closed') continue;
    core.info(`${dryRun ? 'Would delete' : 'Deleting'} ${branch}: ${deployment.id}`);
    // force permits removal of the preview's branch alias; production is excluded above.
    if (!dryRun) await request(`${base}/${encodeURIComponent(deployment.id)}?force=true`, 'DELETE');
    deleted++;
  }
  core.info(`${dryRun ? 'Selected' : 'Deleted'} ${deleted} closed-PR preview deployment(s).`);
};
