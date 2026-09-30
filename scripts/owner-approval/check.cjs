// Loaded only from the trusted default branch by owner-approval.yml.
const OWNER_ID = 4793020;
const STATUS = 'Owner approval';
const protectedTarget = ref => ref === 'master' || ref.startsWith('release/');

function approved(pr, reviews) {
  if (pr.user.id === OWNER_ID) return true;
  const decisive = reviews
    .filter(review => review.user?.id === OWNER_ID &&
      ['APPROVED', 'CHANGES_REQUESTED', 'DISMISSED'].includes(review.state))
    .sort((a, b) => b.id - a.id)[0];
  return decisive?.state === 'APPROVED' && decisive.commit_id === pr.head.sha;
}

async function reconcile({ github, context }) {
  const repo = context.repo;
  const prs = await github.paginate(github.rest.pulls.list, { ...repo, state: 'open', per_page: 100 });
  const groups = new Map();
  for (const pr of prs.filter(pr => protectedTarget(pr.base.ref))) {
    const group = groups.get(pr.head.sha) || [];
    group.push(pr);
    groups.set(pr.head.sha, group);
  }
  for (const [sha, group] of groups) {
    const status = { ...repo, sha, context: STATUS,
      target_url: `https://github.com/${repo.owner}/${repo.repo}/actions/runs/${context.runId}` };
    // Clear any earlier success before reading reviews; API errors leave a blocking status.
    await github.rest.repos.createCommitStatus({ ...status, state: 'pending', description: 'Checking owner approval' });
    let allowed = true;
    for (const snapshot of group) {
      const { data: pr } = await github.rest.pulls.get({ ...repo, pull_number: snapshot.number });
      if (pr.state !== 'open' || pr.head.sha !== sha || !protectedTarget(pr.base.ref)) {
        allowed = false;
        continue;
      }
      const reviews = await github.paginate(github.rest.pulls.listReviews, {
        ...repo, pull_number: pr.number, per_page: 100,
      });
      allowed = approved(pr, reviews) && allowed;
      const { data: current } = await github.rest.pulls.get({ ...repo, pull_number: pr.number });
      allowed = current.state === 'open' && current.head.sha === sha &&
        current.base.ref === pr.base.ref && allowed;
    }
    // Statuses are per commit: every open protected-target PR sharing this SHA must qualify.
    await github.rest.repos.createCommitStatus({ ...status, state: allowed ? 'success' : 'pending',
      description: allowed ? 'Owner-authored or approved by kazo0 at this commit' : 'Waiting for kazo0 to approve the latest commit' });
  }
}

module.exports = { approved, protectedTarget, reconcile };
