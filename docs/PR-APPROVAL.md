# Owner approval gate

The intended merge policy for `master` and `release/**` is:

- PRs authored by `kazo0` (GitHub user ID 4793020) need no reviewer.
- Every other author's PR, including bots and collaborators, needs `kazo0`'s approval on its current head commit. A new commit requires a new approval.
- All five existing required CI checks and resolved review conversations remain required.
- Auto-merge must be enabled on the individual PR. This policy does not enable it automatically.

`owner-approval.yml` publishes the `Owner approval` commit status. It only executes default-branch code and reads current PR/review state from the API. It never checks out or executes PR code. The unprivileged review workflow triggers reconciliation through `workflow_run`; a five-minute schedule and manual dispatch cover missed events, including bot-created PRs. GitHub can delay scheduled runs.

Commit statuses belong to commits, so all open protected-target PRs sharing a head commit must qualify. API errors leave the evaluated commit pending. Reviews by other accounts do not satisfy the policy. Ordinary comments do not revoke approval; dismissal or a later request for changes does.

## Rollout

1. Merge the setup PR under the existing rules. An agent needs the owner's explicit in-the-moment authorization for an admin merge, even for this bootstrap.
2. Dispatch `owner-approval.yml` and verify the status on an open PR. Verify that the status is attributed to GitHub Actions.
3. On the `master` and `release branches merge gate` rulesets, add required status `Owner approval`, restricted to GitHub Actions, while preserving the existing five required checks.
4. Only after the new status requirement is active, set the native required approval count to zero in those same rulesets. Keep the other PR requirements and bypass actors unchanged.
5. Leave the separate `release branches` deletion/force-push protection ruleset unchanged.

Until steps 3–4 are complete, the original one-review requirement still applies. To roll back, restore that requirement before removing the new required status.

## Trust boundary

Like the existing Actions CI gates, this gate trusts repository administrators and collaborators who can publish privileged Actions workflows in this repository. It prevents outside fork code from supplying approval; it is not a separate security boundary against a malicious repository writer with `statuses: write`. The ruleset's existing administrator/App bypasses also remain available, subject to this repository's explicit-authorization policy. Neither bypass is used by this workflow.
