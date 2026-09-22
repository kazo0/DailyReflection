# Web deployment (Cloudflare Pages)

The WebAssembly head is hosted on Cloudflare Pages, where `kazo0.dev` already has its DNS. There are three slots, and each one is a Pages *branch*:

| Slot | Deployed by | Pages branch | URL |
|---|---|---|---|
| Production | `release.yml` `deploy-web`, after `publish` clears the `production` approval gate | `production` | `https://dailyreflection.kazo0.dev` |
| Staging | `alpha.yml` `deploy-web`, on every master merge that touches app code | `staging` | `https://dailyreflection-staging.kazo0.dev` |
| PR preview | `ci.yml` `preview-web`, on every push to a same-repo PR (a sticky comment links it) | `pr-<number>` | `https://pr-<number>.<project>.pages.dev` |

All three call `.github/workflows/web-deploy.yml`, which publishes `net10.0-browserwasm` (Release, profile-guided AOT, no pre-compressed `.br`/`.gz` copies because Pages compresses at the edge) and uploads the `wwwroot` with `wrangler pages deploy --branch <slot>`. Every slot gets the same AOT build, so staging and previews test what production ships. Staging and preview uploads get an extra `X-Robots-Tag: noindex` rule. Caching rules live in `DailyReflection/Platforms/WebAssembly/wwwroot/_headers`.

The deploy job **skips itself until `CLOUDFLARE_API_TOKEN` and `CLOUDFLARE_ACCOUNT_ID` exist**, so the workflows run green before the setup below is done.

## One-time setup

Everything in this section happens in the Cloudflare dashboard, GitHub settings, or a terminal logged into Cloudflare. None of it is in the repo.

1. **Pages project.** The first deploy creates it (named `dailyreflection`, with `production` as its production branch). It's a direct-upload project, not Git-connected, because the build needs .NET and the `wasm-tools` workload. To create it by hand instead:

   ```bash
   npx wrangler login
   npx wrangler pages project create dailyreflection --production-branch production
   ```

   If `dailyreflection.pages.dev` is taken, Pages gives the project a suffixed subdomain. Nothing depends on that name. To use a different project name, set the `CLOUDFLARE_PAGES_PROJECT` repository variable.

2. **Create an API token** (My Profile → API Tokens → Create Custom Token) with **Account → Cloudflare Pages → Edit** on your account only.

3. **Add the GitHub settings** (repo → Settings → Secrets and variables → Actions):
   - secret `CLOUDFLARE_API_TOKEN`: the token from step 2
   - `CLOUDFLARE_ACCOUNT_ID`, as a secret or a variable: the account ID shown on the Workers & Pages overview

   Once both exist, the deploy jobs switch on.

4. **Seed the two named slots** so the custom domains have something to attach to. Either run `alpha.yml` (staging) and `release.yml` (production) once, or upload a local build:

   ```bash
   dotnet publish DailyReflection/DailyReflection.Uno.csproj -f net10.0-browserwasm -c Release -p:TargetFrameworkOverride=wasm -p:CompressionEnabled=false
   npx wrangler pages deploy DailyReflection/bin/Release/net10.0-browserwasm/publish/wwwroot --project-name dailyreflection --branch staging
   npx wrangler pages deploy DailyReflection/bin/Release/net10.0-browserwasm/publish/wwwroot --project-name dailyreflection --branch production
   ```

5. **Production domain.** In Workers & Pages → `dailyreflection` → Custom domains → Set up a custom domain, add `dailyreflection.kazo0.dev`. Cloudflare creates the proxied CNAME to `<project>.pages.dev` and issues the certificate.

6. **Staging domain.** Add `dailyreflection-staging.kazo0.dev` the same way. Then go to DNS → Records, edit its CNAME, and change the target from `<project>.pages.dev` to `staging.<project>.pages.dev`. The record must stay **proxied** (orange cloud), otherwise it serves production ([Cloudflare docs](https://developers.cloudflare.com/pages/how-to/custom-branch-aliases/)).

Keep custom domains one level under `kazo0.dev` (`dailyreflection-staging`, not `staging.dailyreflection`), because Cloudflare's free Universal SSL certificate only covers `*.kazo0.dev`.

## Profile-guided AOT

`dotnet publish` compiles the head with `WasmShellMonoRuntimeExecutionMode=InterpreterAndAOT` (the csproj enables it for every publish, never for `dotnet build` / `dotnet run`). Only the methods listed in `DailyReflection/Platforms/WebAssembly/aot.profile` are compiled to WebAssembly, and the rest of the app stays interpreted. That keeps the download and the build time close to the interpreter's while the code that actually runs, startup included, runs as native WebAssembly. The Uno SDK picks the profile up for every Release build. A publish without it fails (`_RequireWasmAotProfile`), because the mode would otherwise AOT-compile everything, and `web-deploy.yml` checks the output before it uploads anything.

The profile is a recording of one session with the app. A stale profile never breaks the app: methods it doesn't list are just interpreted.

**Every release re-records it.** `release.yml`'s `aot-profile` job runs the recorder below against the release commit, and production web is built with that recording (`deploy-web` passes it to `web-deploy.yml`). The walk's screenshots are uploaded as the `aot-profile-walk` artifact, so check them before approving the `production` gate. The job warns when the new profile is under half the committed one's size, which usually means the walk missed screens. If the recording fails, the store release continues and production web falls back to the committed profile. Once production web is live, `forward-port-aot-profile` commits the profile to the release branch and opens an `aot-profile/vX.Y.Z` PR into master. That PR merges itself once you approve it and CI is green. A PR opened by `GITHUB_TOKEN` gets no `pull_request` CI, so the job dispatches `ci.yml` on the branch instead. Staging and PR previews keep using master's committed profile until that PR merges.

Between releases, re-record by hand when startup or a screen changes substantially (a new page, a new control library, an Uno major version):

```bash
cd scripts/wasm-aot-profile
npm ci
node record.mjs --screenshots /tmp/aot-walk   # ~2 min: profiling publish + a headless Chrome walk
```

The script publishes a profiling build (`-p:UnoGenerateAotProfile=true`) into `artifacts/`, serves it on localhost, and walks every tab, both reading sources, the date pickers, and the sober-time display options at desktop and phone widths in the installed Chrome. It then saves the recording over `aot.profile`. The app draws on a canvas, so the walk clicks fixed coordinates. Check the screenshots after a layout change, fix the coordinates in `record.mjs` if needed, and commit the new `aot.profile`.

To record by hand instead: publish with the same flags as the script, serve `wwwroot`, use the app, press **Shift+Cmd+P** (Shift+Alt+P on Windows/Linux), and move the downloaded `aot.profile` into `Platforms/WebAssembly/`. The profiling build has to be trimmed (`-p:PublishTrimmed=true`). The bootstrapper's default untrimmed profiling build crashes at startup on .NET 10 ("Your mono runtime and class libraries are out of sync").

To publish interpreted for one run, pass `-p:PublishWasmAot=false`.

## Operating notes

- **Rollback:** in the Pages dashboard → Deployments, pick an earlier production deployment → *Rollback to this deployment*. It takes effect immediately and needs no rebuild.
- **Preview cleanup:** Pages keeps every deployment and never deletes preview uploads on its own. They cost nothing and don't count against any storage cap, so there is no GC job (unlike the blog's gh-pages previews). Delete old ones from the dashboard if the list gets long.
- **Previews are public** (anyone with the `pages.dev` URL can open them). If that becomes a problem, put the `*.pages.dev` hostnames behind Cloudflare Access (project → Settings → *Enable access policy*).
- **Caching:** only `package_<hash>/` and the fingerprinted `_framework/` files are cached long-term. `index.html` and `service-worker.js` revalidate on every request, because deployments are atomic and a stale `index.html` would reference a `package_<hash>/` the new deployment no longer contains.
