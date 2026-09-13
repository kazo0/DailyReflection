# One-time CI/CD setup (GitHub Actions)

Everything the repo owner must do **once** before the pipelines in
`.github/workflows/` can ship a release. The workflows themselves are already
in the repo: `ci.yml` (PR/master gate) and `release.yml` (release branches →
approval gate → Google Play + App Store + GitHub release/tag).

> ⚠️ **Identifier check before the first release**: `release.yml` publishes to
> `com.kazo0.dailyreflection` (Play `packageName`, iOS `bundle-id`, fastlane
> `app_identifier`). The csproj's `ApplicationId` must match it — if you change
> one, change the other, and remember the in-place store upgrade only works if
> the ID equals the shipped Xamarin app's ID.

## 1. Gather the credentials

### Android signing (keystore)

The original `key.keystore` that signed the Xamarin app. Azure DevOps secure
files **cannot be re-downloaded** — this must come from your own archives.
You also need the store password, key alias, and key password (the old
pipeline variables `KEYSTORE-PASS`, `KEYSTORE-ALIAS`, `KEY-PASS`).

If the app is enrolled in Play App Signing, a lost *upload* key can be reset
via Play Console support; if not, the keystore is irreplaceable.

### Google Play service account

1. In **Google Cloud Console** (any project): *IAM & Admin → Service Accounts
   → Create service account* (e.g. `github-actions-play`). No cloud roles needed.
2. Create a **JSON key** for it and download the file.
3. In **Play Console**: *Users and permissions → Invite new users* → enter the
   service account's email → grant at least **Release to production** (or
   scope it to the Daily Reflection app).

### Apple certificates & keys

1. **Apple Distribution certificate** exported as `.p12` with its password
   (Keychain Access → export, or create via developer.apple.com → Certificates).
2. An **App Store provisioning profile** for the bundle id must exist at
   developer.apple.com → Profiles (the workflow downloads it fresh by name at
   build time — you don't store the profile itself as a secret).
3. **App Store Connect API key**: App Store Connect → *Users and Access →
   Integrations → App Store Connect API* → generate a key with **App Manager**
   role. Save the `.p8` file (downloadable once), the **Key ID**, and the
   **Issuer ID** shown on that page.

## 2. Set the GitHub secrets and variables

> **Status: done (2026-09-13).** All nine secrets and both variables are set.
> The commands below are the reference for rotating one.

From the repo root (gh CLI, authenticated as `kazo0`), on macOS. Commands
without `--body`/`<` prompt for the value interactively.

```bash
# Android
gh secret set ANDROID_KEYSTORE_BASE64 --body "$(base64 -i ~/path/to/key.keystore)"
gh secret set ANDROID_KEYSTORE_PASSWORD
gh secret set ANDROID_KEY_ALIAS
gh secret set ANDROID_KEY_PASSWORD

# Google Play
gh secret set GOOGLE_PLAY_SERVICE_ACCOUNT_JSON < ~/path/to/service-account.json

# Apple
gh secret set APPLE_CERT_P12_BASE64 --body "$(base64 -i ~/path/to/distribution.p12)"
gh secret set APPLE_CERT_P12_PASSWORD
gh secret set APPSTORE_ISSUER_ID
gh secret set APPSTORE_KEY_ID
gh secret set APPSTORE_PRIVATE_KEY < ~/path/to/AuthKey_XXXXXXXX.p8

# Non-secret variables
gh variable set APPLE_CODESIGN_KEY --body "Apple Distribution: <name> (<TEAMID>)"   # cert common name, exactly as in Keychain
gh variable set APPLE_PROFILE_NAME --body "<App Store provisioning profile name>"
```

`security find-identity -v -p codesigning` prints the certificate common name
to paste into `APPLE_CODESIGN_KEY` verbatim.

Verify with `gh secret list` / `gh variable list`. The names must match the
`secrets.*` / `vars.*` references in `release.yml` exactly — a typo surfaces
only when a release run reaches the signing step.

## 3. Create the approval gate

> **Status: done.** Environment `production` exists with required reviewer
> `kazo0` and a deployment branch policy limited to `release/**`.

Repo **Settings → Environments → New environment** named `production`:

- Add **Required reviewers** → yourself.
- Restrict **deployment branches** to `release/**`, so the publish job can only
  ever run from a release branch.

This is what pauses `release.yml` after the builds and before any store
upload. One approval releases the single publish job.

## 4. Branch rules (the merge gate)

> **Status: done.** Both are **rulesets**, not classic branch protection — the
> classic `.../branches/master/protection` API returns 404 for this repo, which
> is expected. Inspect them with
> `gh api repos/kazo0/DailyReflection/rulesets`.

- **`master`** (id 19730116): pull request required with **1 approving review**,
  review-thread resolution required, squash/rebase merges only, and five
  required status checks — **Unit tests**, **Formatting**, **Build desktop
  (Skia)**, **Build Android (unsigned)**, **Build iOS (simulator)**. Status
  checks must exist before they can be required, so add new ones only after a
  run has reported them. Repository-admin bypass is **enabled** here, which is
  what the "Hard rules" section of `AGENTS.md` is about: a merge or push that
  skips the review is possible for the owner, and is forbidden to agents.
- **`release branches`** (id 23125621): blocks **deletion** and
  **force-push** on `refs/heads/release/**`, with **no bypass actors** — it
  applies to the owner too, deliberately, since these branches are the source
  of shipped builds. Ordinary pushes are unaffected, so the documented hotfix
  flow (commit straight to `release/v4.0`) still works. Deleting a finished
  release branch means deleting or pausing the ruleset first.

## 5. Local tooling

```bash
dotnet tool install -g nbgv    # verified installed 2026-09-13: nbgv 3.10.94
```

## 6. First release (recommended sequence)

1. **Resolve the release blockers first:**
   - ~~`targetSdkVersion=33` in `AndroidManifest.xml`~~ — **resolved (2026-07)**:
     bumped to 36, which clears Google Play's "updates must target API 35+"
     requirement. See the supersession note in spec 009.
   - ~~`Info.plist` shipped a literal `$(ApplicationId)` as `CFBundleIdentifier`~~ —
     **resolved (2026-09)**: the placeholder keys were removed and the .NET iOS SDK
     now fills the bundle identity and versions from the csproj (spec 010
     supersession note). Any `.ipa` built before that fix cannot be uploaded.
   - Confirm the `ApplicationId` question in the warning box above.
2. Merge the feature branch to `master` via PR (CI must be green).
3. On master: `nbgv prepare-release` — creates `release/v4.0` with a stable
   version in `version.json` and bumps master to `4.1-alpha`. Push both:
   `git push origin master release/v4.0`.
4. The push triggers **Release**. Let all build jobs finish, but **don't
   approve yet** — instead do a dry run: *Actions → Release → Run workflow*
   on `release/v4.0` with `play_track=internal` and
   `submit_for_review=false`, then approve that run. This exercises the
   entire pipeline without touching production or App Store review.
5. Check results: build on the Play **internal** track, build in
   **TestFlight**, GitHub release `v4.0.x` with `.aab`/`.apk`/`.ipa`/desktop
   zips attached.
6. When happy, **push a follow-up commit** to the release branch and approve
   that run — it publishes Play **production**, submits the iOS build for
   **App Store review** (auto release on approval), and tags the release.

   Do *not* simply re-run the workflow on the same commit after a dry run.
   The store version numbers are derived from the git height (see "Versioning"
   below), so the same commit always produces the same Android `versionCode`
   and the same iOS `CFBundleVersion` — and both stores reject a re-upload of
   a build number they have already seen, even from a different track. A new
   commit increments the height and sidesteps that.

### Versioning (automatic — nothing to configure)

Version numbers come from Nerdbank.GitVersioning and are computed **in CI, per
build**; no file is hand-edited during a release and the workflow takes no
version input.

- `version.json` at the repo root is the source of truth. `master` carries
  `4.0-alpha`; `nbgv prepare-release` writes the stable `4.0` onto
  `release/v4.0` and bumps master to the next alpha. That command is the one
  manual step, and it runs locally, not in CI.
- `publicReleaseRefSpec` matches `^refs/heads/release/.*$`, so only release
  branches produce clean versions — elsewhere the version carries a
  `-gCOMMITID` suffix, which is what keeps a stray build from looking like a
  shippable one.
- The `version` job in `release.yml` recomputes the version and **fails the
  run** unless `PublicRelease` is `True` and the prerelease tag is empty. A
  branch that was not cut with `nbgv prepare-release` therefore cannot reach
  the stores. Its `SimpleVersion` output names the artifacts, the GitHub
  release, and the `vX.Y.Z` tag.
- The store versions are set by NBGV's own targets during each mobile build —
  `NBGV_SetVersionForMauiAndroid` (before `_GetAndroidPackageName`) and
  `NBGV_SetVersionForMauiIOS` (before `_CompileAppManifest`). They apply to
  this app even though it is not MAUI: the conditions only test
  `TargetPlatformIdentifier`. Android gets
  `versionCode = major<<24 | minor<<16 | height` — 67108873 as of the merge of
  PR #9, comfortably above the Xamarin app's 34 — and iOS gets the three-part
  version for both `CFBundleVersion` and `CFBundleShortVersionString`.
- Every job that builds or computes a version checks out with
  `fetch-depth: 0`; NBGV needs full history to compute the height, and a
  shallow clone would silently change the numbers.

To see what a commit would ship as, run `nbgv get-version` locally, or
`dotnet msbuild DailyReflection/DailyReflection.Uno.csproj -t:_GetAndroidPackageName -p:TargetFramework=net10.0-android -p:TargetFrameworkOverride=android -p:PublicRelease=true -getProperty:ApplicationVersion -getProperty:ApplicationDisplayVersion`
for the exact store values.

Hotfixes: commit to the same `release/v4.0` branch — each push builds a new
`4.0.<height>` and waits for approval again.

## 7. Native AOT store packages

The `.aab`/`.apk` and `.ipa` that `release.yml` ships are **Native AOT**
(<https://platform.uno/docs/articles/features/native-aot.html>) — faster
startup at the cost of a larger package. Nothing to set up: the `PublishAot`
block in `DailyReflection/DailyReflection.Uno.csproj` turns it on for
`dotnet publish` of the mobile TFMs, the workflows pass the `PublishNativeAot`
switch, and the Android job points the SDK at the runner's NDK r27.3
(`ANDROID_NDK_HOME`).
Desktop zips are unchanged (CoreCLR, self-contained).

What to know before the first Native AOT release:

- **Dry-run it first.** The step-6 dry run (`play_track=internal`,
  `submit_for_review=false`) is the moment to install the internal-track build
  and the TestFlight build on real devices and click through all three tabs —
  Native AOT removes code the trimmer cannot see, and a binding to a property
  that was trimmed shows up as an empty control, not a crash. Everyday CI (PRs
  into `master`, pushes to `master`) does **not** run the AOT compile — it is
  slow — so the first Native AOT build of a change happens on the release
  branch itself (push → `release.yml`) or in a PR *into* a `release/*` branch,
  where `ci.yml` switches to the same unsigned AOT publish.
- **Falling back.** Run the workflow manually with `native_aot=false` to ship
  the same commit with the runtime's own Mono AOT instead. Do this rather than
  editing the csproj on the release branch.
- **Android is "experimental" per Microsoft.** The .NET for Android SDK prints
  warning `XA1040` on every Native AOT publish; Uno Platform documents and
  ships it. It only affects the 64-bit ABIs the app already builds
  (`android-arm64;android-x64` are the SDK defaults — no device coverage lost).
- **Reproducing a store build locally** (commands in `AGENTS.md`, "Native AOT
  publish"): iOS needs Xcode and the `ios` workload; Android additionally
  needs an NDK r27+ (`sdkmanager --install "ndk;27.3.13750724"` matches CI).
  Add Uno's diagnostic flags (`-p:TrimmerSingleWarn=false
  -p:_ExtraTrimmerArgs=--verbose -p:IlcGenerateMetadataLog=true
  -p:IlcGenerateMstatFile=true`) to see exactly what was trimmed and which
  members remain reachable by reflection.

## Reference: what lives where

| Thing | Location |
|---|---|
| Version source of truth | `version.json` (repo root, Nerdbank.GitVersioning) |
| versionCode scheme | NBGV built-in: `major<<24 \| minor<<16 \| height` (4.0.x ⇒ 67108864+x; must stay above Xamarin's 34) |
| Store version mapping | NBGV targets `NBGV_SetVersionForMauiAndroid` / `NBGV_SetVersionForMauiIOS` (see comment in `DailyReflection/Directory.Build.props`) |
| Merge gate | `.github/workflows/ci.yml` + the `master` ruleset (5 required checks, 1 approval) |
| Release-branch guard | The `release branches` ruleset (no deletion, no force-push, no bypass) |
| Release pipeline | `.github/workflows/release.yml` (trigger: push to `release/**`) |
| Native AOT switch | `PublishAot` block in `DailyReflection/DailyReflection.Uno.csproj`; per-run override via the `native_aot` dispatch input (or `-p:PublishNativeAot=false` locally) |
| Approval gate | GitHub Environment `production` |
| Secrets/variables | GitHub repo Settings → Secrets and variables → Actions |
