const { test } = require('node:test');
const assert = require('node:assert/strict');
const cleanup = require('./cleanup.cjs');
const deployment = (id, branch, environment = 'preview') => ({ id, environment, deployment_trigger: { metadata: { branch } } });

function harness(pages, states = {}, options = {}) {
  const calls = [], lookups = [], logs = [];
  const args = {
    env: { CLOUDFLARE_API_TOKEN: 'test', CLOUDFLARE_ACCOUNT_ID: 'account', PAGES_PROJECT: 'project', ...options.env },
    context: { repo: { owner: 'owner', repo: 'repo' }, eventName: 'schedule', ...options.context },
    core: { info: s => logs.push(s), notice: s => logs.push(s) },
    github: { rest: { pulls: { get: async ({ pull_number }) => {
      lookups.push(pull_number);
      const state = states[pull_number];
      if (!state) throw new Error('PR lookup failed');
      return { data: { state: typeof state === 'function' ? state() : state } };
    } } } },
    fetchImpl: async (url, { method }) => {
      calls.push({ url, method });
      if (options.fail === method) return { ok: false, status: 403, json: async () => ({ success: false, errors: [{ code: 10000 }] }) };
      const page = Number(new URL(url).searchParams.get('page'));
      return { ok: true, status: 200, json: async () => ({ success: true, result: method === 'GET' ? (pages[page - 1] || []) : {} }) };
    },
  };
  return { args, calls, lookups, logs, run: () => cleanup(args), deletes: () => calls.filter(c => c.method === 'DELETE') };
}

test('sweep paginates before deleting and preserves open PRs, staging, production and unrelated branches', async () => {
  const h = harness([
    [deployment('a', 'pr-1'), deployment('b', 'staging'), deployment('c', 'pr-2')],
    [deployment('d', 'pr-1'), deployment('e', 'production'), deployment('f', 'pr-1', 'production'), deployment('g', 'pr-1-extra')],
  ], { 1: 'closed', 2: 'open' });
  await h.run();
  assert.deepEqual(h.calls.map(c => c.method), ['GET', 'GET', 'GET', 'DELETE', 'DELETE']);
  assert.ok(h.calls.filter(c => c.method === 'GET').every(c => new URL(c.url).searchParams.get('per_page') === '25'));
  assert.deepEqual(h.deletes().map(c => new URL(c.url).pathname.split('/').pop()), ['a', 'd']);
  assert.ok(h.deletes().every(c => c.url.endsWith('?force=true')));
});

test('close event only removes its own PR, merged or unmerged', async () => {
  const h = harness([[deployment('a', 'pr-1'), deployment('b', 'pr-2')]], { 1: 'closed', 2: 'closed' }, {
    context: { eventName: 'pull_request_target', payload: { pull_request: { number: 2 } } },
  });
  await h.run();
  assert.deepEqual(h.lookups, [2]);
  assert.equal(h.deletes().length, 1);
});

test('dry run and missing configuration never delete', async () => {
  const h = harness([[deployment('a', 'pr-1')]], { 1: 'closed' }, { env: { DRY_RUN: 'true' } });
  await h.run();
  assert.equal(h.deletes().length, 0);
  assert.ok(h.logs.some(s => s.includes('Would delete')));
  const missing = harness([], {}, { env: { CLOUDFLARE_API_TOKEN: '' } });
  await missing.run();
  assert.equal(missing.calls.length, 0);
});

test('reopened PR is preserved, including between deletions', async () => {
  let count = 0;
  const h = harness([[deployment('a', 'pr-1'), deployment('b', 'pr-1')]], { 1: () => ++count === 1 ? 'closed' : 'open' });
  await h.run();
  assert.equal(h.deletes().length, 1);
});

test('lookup and Cloudflare failures surface instead of silently succeeding', async () => {
  const missing = harness([[deployment('a', 'pr-1')]]);
  await assert.rejects(missing.run(), /PR lookup failed/);
  assert.equal(missing.deletes().length, 0);
  for (const fail of ['GET', 'DELETE']) {
    const h = harness([[deployment('a', 'pr-1')]], { 1: 'closed' }, { fail });
    await assert.rejects(h.run(), /HTTP 403/);
  }
});
