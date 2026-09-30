const { test } = require('node:test');
const assert = require('node:assert/strict');
const { approved, protectedTarget, reconcile } = require('./check.cjs');

const pr = (number = 1, userId = 123, sha = 'head') => ({
  number, user: { id: userId, login: 'kazo0' }, head: { sha }, base: { ref: 'master' }, state: 'open',
});
const review = (id, state = 'APPROVED', commit = 'head', userId = 4793020) => ({
  id, state, commit_id: commit, user: { id: userId },
});

test('only the immutable owner account ID grants the author exception', () => {
  assert.equal(approved(pr(1, 4793020), []), true);
  assert.equal(approved(pr(), []), false);
});
test('outside PRs need the owner approval on their current commit', () => {
  assert.equal(approved(pr(), [review(1)]), true);
  assert.equal(approved(pr(), [review(1, 'APPROVED', 'old')]), false);
  assert.equal(approved(pr(), [review(1, 'APPROVED', 'head', 456)]), false);
});
test('comments preserve approval, while dismissal and change requests revoke it', () => {
  assert.equal(approved(pr(), [review(2, 'COMMENTED'), review(1)]), true);
  assert.equal(approved(pr(), [review(1), review(2, 'CHANGES_REQUESTED')]), false);
  assert.equal(approved(pr(), [review(1, 'DISMISSED')]), false);
  assert.equal(approved(pr(), [review(3), review(2, 'CHANGES_REQUESTED')]), true);
});
test('both protected branch families are included', () => {
  assert.equal(protectedTarget('master'), true);
  assert.equal(protectedTarget('release/v4.0'), true);
  assert.equal(protectedTarget('feature/release/test'), false);
});

function fixture(prs, reviews = [], { error, changedHead } = {}) {
  const statuses = [];
  let gets = 0;
  const github = {
    rest: {
      pulls: {
        list: 'list', listReviews: 'reviews',
        get: async ({ pull_number }) => {
          gets++;
          const current = structuredClone(prs.find(p => p.number === pull_number));
          if (changedHead && gets > 1) current.head.sha = 'new-head';
          return { data: current };
        },
      },
      repos: { createCommitStatus: async status => statuses.push(status) },
    },
    paginate: async method => {
      if (method === 'list') return prs;
      if (error) throw new Error('API unavailable');
      return reviews;
    },
  };
  return { statuses, input: { github, context: { repo: { owner: 'kazo0', repo: 'DailyReflection' }, runId: 1 } } };
}
test('owner PR succeeds and outside unapproved PR remains pending', async () => {
  const f = fixture([pr(1, 4793020, 'owner'), pr(2)]);
  await reconcile(f.input);
  assert.deepEqual(f.statuses.map(s => [s.sha, s.state]), [
    ['owner', 'pending'], ['owner', 'success'], ['head', 'pending'], ['head', 'pending'],
  ]);
});
test('same-SHA owner PR cannot grant approval to an outside PR', async () => {
  const f = fixture([pr(1, 4793020), pr(2)]);
  await reconcile(f.input);
  assert.equal(f.statuses.at(-1).state, 'pending');
});
test('owner approval allows an outside PR', async () => {
  const f = fixture([pr()], [review(1)]);
  await reconcile(f.input);
  assert.equal(f.statuses.at(-1).state, 'success');
});
test('API failures invalidate an earlier success before reading reviews', async () => {
  const f = fixture([pr()], [], { error: true });
  await assert.rejects(reconcile(f.input), /API unavailable/);
  assert.deepEqual(f.statuses.map(s => s.state), ['pending']);
});
test('a push during evaluation cannot receive success', async () => {
  const f = fixture([pr()], [review(1)], { changedHead: true });
  await reconcile(f.input);
  assert.equal(f.statuses.at(-1).state, 'pending');
});
