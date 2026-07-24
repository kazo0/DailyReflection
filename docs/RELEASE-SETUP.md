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
   - `targetSdkVersion=33` in `AndroidManifest.xml` — Google Play rejects
     updates targeting < API 35. Needs a spec-009-reviewed bump before the
     Play upload can succeed.
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

## Reference: what lives where

| Thing | Location |
|---|---|
| Version source of truth | `version.json` (repo root, Nerdbank.GitVersioning) |
| versionCode scheme | NBGV built-in: `major<<24 \| minor<<16 \| height` (4.0.x ⇒ 67108864+x; must stay above Xamarin's 34) |
| Store version mapping | NBGV targets `NBGV_SetVersionForMauiAndroid` / `NBGV_SetVersionForMauiIOS` (see comment in `DailyReflection/Directory.Build.props`) |
| Merge gate | `.github/workflows/ci.yml` + branch protection on `master` |
| Release pipeline | `.github/workflows/release.yml` (trigger: push to `release/**`) |
| Approval gate | GitHub Environment `production` |
| Secrets/variables | GitHub repo Settings → Secrets and variables → Actions |
