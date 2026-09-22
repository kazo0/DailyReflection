# Web deployment (Cloudflare Pages)

The WebAssembly head is hosted on Cloudflare Pages, where `kazo0.dev` already has its DNS. There are three slots, and each one is a Pages *branch*:

| Slot | Deployed by | Pages branch | URL |
|---|---|---|---|
| Production | `release.yml` `deploy-web`, after `publish` clears the `production` approval gate | `production` | `https://dailyreflection.kazo0.dev` |
| Staging | `alpha.yml` `deploy-web`, on every master merge that touches app code | `staging` | `https://dailyreflection-staging.kazo0.dev` |
| PR preview | `ci.yml` `preview-web`, on every push to a same-repo PR (a sticky comment links it) | `pr-<number>` | `https://pr-<number>.<project>.pages.dev` |

All three call `.github/workflows/web-deploy.yml`, which publishes `net10.0-browserwasm` (Release, no pre-compressed `.br`/`.gz` copies because Pages compresses at the edge) and uploads the `wwwroot` with `wrangler pages deploy --branch <slot>`. Staging and preview uploads get an extra `X-Robots-Tag: noindex` rule. Caching rules live in `DailyReflection/Platforms/WebAssembly/wwwroot/_headers`.

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

## Operating notes

- **Rollback:** in the Pages dashboard → Deployments, pick an earlier production deployment → *Rollback to this deployment*. It takes effect immediately and needs no rebuild.
- **Preview cleanup:** Pages keeps every deployment and never deletes preview uploads on its own. They cost nothing and don't count against any storage cap, so there is no GC job (unlike the blog's gh-pages previews). Delete old ones from the dashboard if the list gets long.
- **Previews are public** (anyone with the `pages.dev` URL can open them). If that becomes a problem, put the `*.pages.dev` hostnames behind Cloudflare Access (project → Settings → *Enable access policy*).
- **Caching:** only `package_<hash>/` and the fingerprinted `_framework/` files are cached long-term. `index.html` and `service-worker.js` revalidate on every request, because deployments are atomic and a stale `index.html` would reference a `package_<hash>/` the new deployment no longer contains.
