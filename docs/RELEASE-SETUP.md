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

From the repo root (gh CLI, already authenticated as `kazo0`). Commands
without `--body`/`<` prompt for the value interactively.

```powershell
# Android
gh secret set ANDROID_KEYSTORE_BASE64 --body ([Convert]::ToBase64String([IO.File]::ReadAllBytes("C:\path\to\key.keystore")))
gh secret set ANDROID_KEYSTORE_PASSWORD
gh secret set ANDROID_KEY_ALIAS
gh secret set ANDROID_KEY_PASSWORD

# Google Play
gh secret set GOOGLE_PLAY_SERVICE_ACCOUNT_JSON < C:\path\to\service-account.json

# Apple
gh secret set APPLE_CERT_P12_BASE64 --body ([Convert]::ToBase64String([IO.File]::ReadAllBytes("C:\path\to\distribution.p12")))
gh secret set APPLE_CERT_P12_PASSWORD
gh secret set APPSTORE_ISSUER_ID
gh secret set APPSTORE_KEY_ID
gh secret set APPSTORE_PRIVATE_KEY < C:\path\to\AuthKey_XXXXXXXX.p8

# Non-secret variables
gh variable set APPLE_CODESIGN_KEY --body "Apple Distribution: <name> (<TEAMID>)"   # cert common name, exactly as in Keychain
gh variable set APPLE_PROFILE_NAME --body "<App Store provisioning profile name>"
```

Verify with `gh secret list` / `gh variable list`.

## 3. Create the approval gate

Repo **Settings → Environments → New environment** named `production`:

- Add **Required reviewers** → yourself.

This is what pauses `release.yml` after the builds and before any store
upload. One approval releases the single publish job.

## 4. Branch protection (the merge gate)

After the first CI run has executed (check names must exist first):

**Settings → Branches → Add branch protection rule** for `master`:

- Require status checks to pass: **Unit tests**, **Build desktop (Skia)**,
  **Build Android (unsigned)**, **Build iOS (simulator)**.
- Optional: a ruleset preventing deletion/force-push of `release/*` branches.

## 5. Local tooling

```powershell
dotnet tool install -g nbgv    # already installed on this machine
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
6. When happy, re-run via *Run workflow* with defaults (or push a follow-up
   commit to the release branch) and approve — that publishes Play
   **production**, submits the iOS build for **App Store review** (auto
   release on approval), and tags the release.

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

## 8. Alpha builds on every master merge

`.github/workflows/alpha.yml` builds a signed Native AOT `.aab` and `.ipa` on
every push to `master` that touches app code, and pushes them to the Google
Play **internal** track and **TestFlight**. Docs-only merges are skipped
(`paths-ignore`), because each upload consumes a store build number.

There is no approval gate: the internal track and TestFlight are the only
destinations, and the iOS build is uploaded with `fastlane pilot` rather than
`deliver`, so it never touches the App Store version record or the review
queue. `release.yml` remains the only path to a public release.

One-time setup on the store side:

- **TestFlight**: App Store Connect → *TestFlight → Internal Testing* → add
  yourself to a group with *automatically distribute builds* enabled.
  Otherwise builds arrive and sit there unassigned. Apple processes each
  upload for a few minutes before it appears.
- **Play**: Play Console → *Testing → Internal testing* → add your Google
  account as a tester and accept the opt-in link once. The service account
  already has upload rights from §1.

Two things to know:

- **The version train matters.** Build numbers come from the git height, so an
  alpha build from `master` and a build from a `release/*` branch on the same
  version can collide, and the store rejects the second one. Keeping `master`
  on the next `-alpha` version (which `nbgv prepare-release` does automatically
  when you cut a release) keeps the two trains apart. Cut `release/v4.0` before
  relying on this workflow.
- **It is slow on purpose.** Native AOT means roughly 10 minutes for Android
  and 25-30 for iOS per merge. A plain build would be far quicker but would not
  exercise the trimmer, which is the whole reason these builds exist.

## Reference: what lives where

| Thing | Location |
|---|---|
| Version source of truth | `version.json` (repo root, Nerdbank.GitVersioning) |
| versionCode scheme | NBGV built-in: `major<<24 \| minor<<16 \| height` (4.0.x ⇒ 67108864+x; must stay above Xamarin's 34) |
| Store version mapping | NBGV targets `NBGV_SetVersionForMauiAndroid` / `NBGV_SetVersionForMauiIOS` (see comment in `DailyReflection/Directory.Build.props`) |
| Merge gate | `.github/workflows/ci.yml` + branch protection on `master` |
| Release pipeline | `.github/workflows/release.yml` (trigger: push to `release/**`) |
| Native AOT switch | `PublishAot` block in `DailyReflection/DailyReflection.Uno.csproj`; per-run override via the `native_aot` dispatch input (or `-p:PublishNativeAot=false` locally) |
| Approval gate | GitHub Environment `production` |
| Secrets/variables | GitHub repo Settings → Secrets and variables → Actions |
