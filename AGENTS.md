# AGENTS.md

Guidance for AI coding agents working in this repository. Read this first; it assumes no prior knowledge of the project.

## Hard rules

**Never use the owner's admin rights to bypass branch protection.** This is not a
default to weigh against convenience — an agent may not do it on its own under any
circumstance, including when the change is small, urgent, obviously correct, or
fixing something the agent itself broke.

`master` is protected (pull request required, 1 approving review, 4 required status
checks), but `enforce_admins` is **false**. That means a plain `git push origin
master` as the owner *succeeds* and GitHub reports `Bypassed rule violations` after
the fact. There is no `--force` involved and no prompt — the guardrail simply does
not apply to this account. Treat that push as forbidden, not as permitted-because-
it-worked.

Specifically forbidden without an explicit, in-the-moment instruction from the owner:

- pushing directly to `master` or any `release/*` branch
- `gh pr merge --admin`, or any merge that skips required reviews or status checks
- `git push --force` / `--force-with-lease` to `master` or a `release/*` branch
- changing branch protection, rulesets, or `enforce_admins` to make a push possible

The path is always: **branch → pull request → checks go green → the owner merges.**
If that path is blocked, stop and say so. Do not route around it. Reporting "I could
not land this without a bypass" is the correct outcome; landing it is not.

Force-pushing a *tag* in a tooling repo (e.g. moving `v1`) is a different thing and
is fine. This rule is about protected branches in this repository.

## Project overview

**Daily Reflection** is a cross-platform mobile/desktop app that shows daily excerpts from a book of reflections by A.A. members, tracks the user's sobriety time, and schedules a daily reminder notification. It is published on the Apple App Store and Google Play (`com.kazo0.dailyreflection`).

This branch contains the **Uno Platform port** of the original Xamarin.Forms app ([kazo0/DailyReflection](https://github.com/kazo0/DailyReflection)). The port is designed to upgrade the Xamarin app **in place** on the stores: it keeps the original `ApplicationId` (`com.kazo0.dailyreflection`) and moves to 4.x versions computed by Nerdbank.GitVersioning (packed Android versionCode ≥ 67108864, above the Xamarin app's 3.4 (34)). On first launch after the upgrade, version-gated startup migrations import user settings (sober date, notification time/enabled, display preference) from the legacy platform stores (Android SharedPreferences, iOS `DR_Settings` NSUserDefaults suite) and re-schedule the daily notification.

The UX is three tabs: **Reflection** (daily reading, date picker, share), **Sober Time** (years/months/days since sober date), **Settings** (sober date, notification time, display preference, app theme). A hard requirement of the port (see `prompt.md`) is *no visual redesign* — UI work restores the original Xamarin Shell behaviour, it does not refresh it.

## Technology stack

- **.NET 10** — shared libraries target `net10.0`; the app head targets `net10.0-android`, `net10.0-ios`, `net10.0-desktop`.
- **Uno Platform 6.x** single project (`Uno.Sdk` pinned in `global.json`; `allowPrerelease: false`). Enabled `UnoFeatures`: `SkiaRenderer`, `Hosting`, `Toolkit`, `Material`, `Configuration`, `Navigation`, `Mvux`.
- **WinUI 3 XAML** rendered by the Uno Skia renderer; **Uno Material** theme (`MaterialToolkitTheme` in `App.xaml` with `Styles/ColorPaletteOverride.xaml` mapping the Xamarin-era DR palette onto Material color keys — primary stays `#1976D2`); **Uno.Toolkit `TabBar`** for the tab chrome and **Uno.Toolkit `NavigationBar`** as the top app bar on every page.
- **Uno.Extensions** — generic host (`Microsoft.Extensions.Hosting`), region-based Navigation (Visibility navigator), Configuration (embedded `appsettings.json`).
- **MVUX (Uno.Extensions.Reactive 7.1.1)** — presentation is `partial record` models with `IFeed`/`IState` + generated `Bindable*Model` view-models; no CommunityToolkit, no messenger, no hand-written `INotifyPropertyChanged`.
- **sqlite-net-pcl 1.10.196-beta + SQLitePCLRaw.bundle_e_sqlite3** — read-only embedded SQLite database (`dailyreflections.db`) extracted to `LocalApplicationData` on first run.
- **NodaTime 3.0.3** — sober-period arithmetic (`Period.Between`).
- **NUnit 4 + Moq** for unit tests.
- `.mcp.json` configures the Uno App dev-server MCP (`uno.devserver`) and the Uno Platform docs MCP for IDE agents.

## Solution layout

`DailyReflection.slnx` is the solution (XML format) with 7 projects.

```
DailyReflection.Core           Constants (AutomationConstants, PreferenceConstants, VersionConstants,
                               ConfigurationConstants) + extensions (ServiceCollectionExtensions with the
                               AddAllSubclassesOf<T> DI helper, HtmlInlineParser, StringExtensions). No project deps.
DailyReflection.Data           Reflection model, SoberTimeDisplayPreference / AppThemePreference enums, IDailyReflectionDatabase /
                               DailyReflectionDatabase (SQLite). dailyreflections.db is an EmbeddedResource.
DailyReflection.Services       Service interfaces (ISettingsService, IShareService, INotificationService,
                               IVersionTrackingService, IAppThemeService), IDailyReflectionService implementation, and
                               StartupMigrationRunner (version-gated settings import + DB refresh).
DailyReflection.Presentation   MVUX models (partial records: DailyReflectionModel, SobrietyTimeModel,
                               SettingsModel) exposing IFeed/IState; the MVUX generator emits Bindable*Model
                               view-models + ReactiveViewModelMappings into this assembly. References
                               Uno.Extensions.Reactive(.WinUI) 7.1.1. AddPresentationDependencies is the DI entry.
DailyReflection                The Uno head (DailyReflection.Uno.csproj). Entry point App.OnLaunched().
  ├─ Views/                    MainPage (TabBar shell) + DailyReflectionPage / SobrietyTimePage / SettingsPage,
  │                            each with a Toolkit NavigationBar; the reflection page uses an MVUX FeedView.
  ├─ PlatformServices/         Partial-class implementations of the Services interfaces, split per platform by
  │                            filename suffix: .Android.cs / .iOS.cs / .Windows.cs / .Desktop.cs.
  ├─ Platforms/                Per-platform entry points and manifests (AndroidManifest.xml, Info.plist,
  │                            Android BroadcastReceivers for the daily alarm).
  ├─ Converters/               IValueConverter implementations used by the XAML, plus the HtmlEx attached
  │                            property (renders the DB's inline HTML into TextBlock.Inlines).
  ├─ Styles/                   ColorPaletteOverride.xaml (DR palette → Uno Material color keys), PickerFlyouts.xaml
  │                            (Material-styled date/time picker flyouts) and SettingsComboBox.xaml
  │                            (DRSettingsComboBoxStyle — the Settings enum pickers as card-look ComboBoxes).
  ├─ Strings/en, Assets/       Localization resources and image assets.
  └─ appsettings.json          Embedded config; the required key is DatabaseFileName (App fails fast if missing).
DailyReflection.Presentation.Tests   NUnit + Moq tests of the MVUX models (net10.0; base class ModelTestBase).
DailyReflection.Services.Tests       NUnit + Moq tests of services, startup migrations, HTML parser, plus
                                     lint-style tests (automation-ID coverage, XAML surface shape).
DailyReflection.UITests              LEGACY .NET Framework 4.8 Xamarin.UITest scaffold. NOT in the solution;
                                     its csproj references a Xamarin-era project path that no longer exists.
                                     Treat it as read-only reference for AutomationId-based UI testing.
```

### Architecture rules worth knowing

- **The four shared libraries are platform-agnostic.** There are no `#if __ANDROID__` / `#if __IOS__` blocks in Core/Data/Services/Presentation. All platform variation lives in the head, in `PlatformServices/` partial classes and `Platforms/`. Keep it that way.
- **DI registration chain** (all via `Microsoft.Extensions.DependencyInjection` extension methods):
  `Platform.AddPlatformServices()` (head: settings/share/notification/version-tracking/theme) → `Presentation.AddPresentationDependencies()` (the three MVUX models **and** the generated `Bindable*Model` view-models that wrap them, all Singleton — registering the bindables matters: the navigator resolves view models from DI first, and its fallback construction would new up a second, disconnected instance of the model) → `Services.AddServiceDependencies()` (`IDailyReflectionService` Transient) → `Data.AddDataDependencies()` (`IDailyReflectionDatabase` Singleton).
- **Navigation**: routes are registered in `App.RegisterRoutes` — `Main` (default) with nested routes `Reflection` (default), `SoberTime`, `Settings`. `MainPage` hosts a `TabBar` whose items map to region names; the content region uses the Visibility navigator (tabs are loaded lazily and toggled, not frame-navigated). `OnLaunched` uses the MVUX overload `UseNavigation(ReactiveViewModelMappings.ViewModelMappings, RegisterRoutes)` so the navigator wraps the DI-resolved model in its generated `Bindable*Model` and sets that as the page's DataContext — pages never set DataContext themselves. Pages are registered Transient, models/bindables Singleton — deliberate, see the comments in `App.xaml.cs` and `AddPresentationDependencies`.
- **Cross-model sync is feed composition, not a messenger.** `SobrietyTimeModel` takes the singleton `SettingsModel` and projects its states (`SoberDate`, `SoberTimeDisplayPreference`) into flat feeds via `Select` — a Settings edit re-derives the Sober Time tab automatically. There is no `WeakReferenceMessenger`/`Messages/` infrastructure anymore.
- **MVUX models** are `partial record`s exposing `IFeed<T>` (read-only) and `IState<T>` (two-way-bound). Side effects (persistence, notification scheduling) run as `ForEach` callbacks that compare the new value against the last-persisted one so the subscription's initial replay is a no-op — startup must stay side-effect free (`StartupMigrationRunner` owns startup re-scheduling). Public `ValueTask` methods on a model become generated commands bound from XAML (`{Binding Share}`). Classic `{Binding}` is used in the views (the navigator owns DataContext); a bindable's feed unwraps at the leaf of the path only, so models expose flat feeds rather than nested record paths.
- **App theme** (System / Light / Dark) is `SettingsModel.AppThemePreference`, an `IState` persisted like the other settings (`PreferenceConstants.AppThemePreference`, stored as the enum's `int`). Its `ForEach` side effect calls `IAppThemeService.ApplyTheme`; the head's `AppThemeService` sets `RequestedTheme` on the window's root element — always an explicit Light/Dark; `System` resolves to the current OS theme and is re-applied on `UISettings.ColorValuesChanged` (resetting the root to `ElementTheme.Default` leaves style-supplied ThemeResources stale on the next OS change in Uno, so don't). Because the model's startup replay is side-effect free, `App.OnLaunched` applies the persisted choice itself, right after `NavigateAsync` has given the window a root.
- **Startup migrations** (`StartupMigrationRunner`) run after first paint and are version-gated by `VersionConstants`; they port the Xamarin `App.OnStart` behaviour. Be careful here — they touch the store-upgrade path for real users.
- Code comments reference design docs by spec number and section (e.g. `Spec 004 §D`) — see `specs/`.

## Build and test commands

Requires the **.NET 10 SDK** (10.0.110 verified working). Building the Android/iOS TFMs requires the corresponding .NET mobile workloads; the desktop TFM and the test projects do not.

```bash
# Build everything in the solution (needs Android + iOS workloads installed)
dotnet build DailyReflection.slnx

# Build the head for desktop only — fastest check. TargetFrameworkOverride keeps restore
# off the mobile TFMs, so no Android/iOS workloads are needed (see below).
# (verified: builds clean, 0 errors, ~16 s on .NET SDK 10.0.110)
dotnet build DailyReflection/DailyReflection.Uno.csproj -f net10.0-desktop -p:TargetFrameworkOverride=desktop

# Run the app on desktop
dotnet run --project DailyReflection/DailyReflection.Uno.csproj -f net10.0-desktop -p:TargetFrameworkOverride=desktop

# Unit tests (NUnit) — verified green: 35 presentation + 31 services = 66 tests
dotnet test DailyReflection.Presentation.Tests/DailyReflection.Presentation.Tests.csproj
dotnet test DailyReflection.Services.Tests/DailyReflection.Services.Tests.csproj

# Run a single test
dotnet test DailyReflection.Presentation.Tests/DailyReflection.Presentation.Tests.csproj --filter "FullyQualifiedName~TestMethodName"
```

### Building a single platform

`DailyReflection.Uno.csproj` is the only crosstargeted project (`net10.0-android;net10.0-ios;net10.0-desktop`); the four shared libraries and the tests are plain `net10.0`. Passing `-f` alone still makes **restore** resolve every TFM, which requires the mobile workloads even for a desktop-only build. `TargetFrameworkOverride` narrows the project itself, so restore and build only ever see the platforms you asked for:

```bash
# Platform suffixes: android, ios, desktop. Semicolon-separated for more than one — on the
# command line write the separator as %3B (MSBuild splits -p: values on a literal ';', even quoted).
dotnet build DailyReflection/DailyReflection.Uno.csproj -c Release -p:TargetFrameworkOverride=desktop
dotnet build DailyReflection/DailyReflection.Uno.csproj -c Release -p:TargetFrameworkOverride=android%3Bdesktop
```

The csproj expands each suffix to its versioned TFM; unset (the default) means all three. It is also read from the environment, which is how each CI job pins itself to one platform (`env: TargetFrameworkOverride: desktop` in `.github/workflows/*.yml`).

For local/IDE builds, copy `crosstargeting_override.props.sample` (repo root) → `crosstargeting_override.props` and uncomment the platform you want; the file is git-ignored and imported by `DailyReflection/Directory.Build.props`. **Close the IDE before changing it** — switching platforms while the solution is open corrupts the NuGet restore cache.

The Uno.Sdk version comes from `global.json` — update it there, not in the csproj. The head uses **central package management** (`ManagePackageVersionsCentrally` in `DailyReflection/Directory.Build.props`, versions in `DailyReflection/Directory.Packages.props`); the shared libraries pin package versions inline in their own csproj files.

## Code style guidelines

There is **no `.editorconfig`** in this repo (older docs claim otherwise — that is stale). The observed conventions, which you should match per-file rather than restyle:

- **Line endings are managed by git** (`.gitattributes`: `* text=auto`): the repository stores LF, so a Windows checkout sees CRLF and a macOS/Linux checkout sees LF. Leave endings alone — never convert files by hand.
- **Indentation is split by layer**: the shared libraries (`DailyReflection.Core/Data/Services/Presentation` and their tests) use **tabs**; the Uno head (`DailyReflection/`) uses **4 spaces**. Match the file you are editing.
- `Nullable` is enabled and `LangVersion` is `Latest` in all projects; the head also has `ImplicitUsings` (see its `GlobalUsings.cs`).
- File-scoped namespaces (`namespace Foo;`) are used throughout.
- PascalCase types/members, `I`-prefixed interfaces, `_camelCase` private fields.
- Minimal diffs: this codebase values parity with the Xamarin original over refactoring. Do not introduce new styles, controls, or design language (hard constraint from `prompt.md`).
- New UI elements that are user-interactive should get `AutomationProperties.AutomationId` values from `DailyReflection.Core/Constants/AutomationConstants.cs` — a lint test fails if a constant is not referenced by any view.

## Testing instructions

- Unit tests are NUnit 4 + Moq on `net10.0`, split by layer: `DailyReflection.Presentation.Tests` (MVUX model behaviour — feeds/states, settings persistence + notification side effects, share-closure invariant; base class `ModelTestBase` with an `Eventually` poll helper for async dispatch) and `DailyReflection.Services.Tests` (service plumbing, startup-migration version gates, HTML inline parser; base class `ServiceTestBase`).
- The Services tests include repo-level lint tests: `AutomationConstantsCoverageTests` (every automation-ID constant is used in at least one XAML view) and `ViewSurfaceTests` (XAML binding contract / NavigationBar / FeedView / theme-brush assertions). When you change XAML structure or automation IDs, run these.
- `DailyReflection.UITests` is a legacy Xamarin.UITest (.NET Framework 4.8) scaffold that is **not buildable** in the current tree (not in the solution; references a removed Xamarin project). Use it only as a reference for how view-level UI tests were structured (page-object pattern keyed on AutomationIds).
- All 66 unit tests pass on .NET SDK 10.0.103 as of this writing; keep them green.

## Deployment / CI

- CI/CD is **GitHub Actions**:
  - `.github/workflows/ci.yml` — the merge gate for PRs and `master`: unit tests, desktop build, unsigned Android build, iOS simulator build. These four jobs are intended to be required status checks on `master`.
  - Every job that builds the head pins itself to one platform with a job-level `env: TargetFrameworkOverride: <android|ios|desktop>` (see "Building a single platform"), so a job only restores the TFM it builds and only needs that platform's workload.
  - `.github/workflows/release.yml` — triggered by any push to a `release/*` branch: computes/validates the version, runs tests, builds a signed `.aab`/`.apk`, a signed `.ipa`, and self-contained desktop zips (win-x64 / linux-x64 / osx-arm64), then **waits for manual approval** on the `production` GitHub Environment before uploading to Google Play, uploading + submitting to App Store Connect (fastlane `deliver`), and creating a GitHub release — which pushes the `vX.Y.Z` tag. `workflow_dispatch` inputs allow dry runs (Play test track, skip App Store review submission).
- **Versioning is Nerdbank.GitVersioning** (`version.json` at the repo root; master carries `X.Y-alpha`). Cut release branches with `nbgv prepare-release` (creates `release/vX.Y` with the stable version and bumps master to the next `-alpha`). NBGV's built-in mobile targets (`NBGV_SetVersionForMauiAndroid`/`IOS`) set the store versions: Android versionCode = `major<<24 | minor<<16 | git height` and versionName = the semantic version; iOS uses the three-part version for `CFBundleVersion`/`CFBundleShortVersionString`. Do not hardcode `ApplicationVersion`/`ApplicationDisplayVersion` in the csproj, and never switch to a scheme that produces smaller versionCodes once a release has shipped.
- **Store identity is load-bearing**: `ApplicationId` must stay `com.kazo0.dailyreflection` and the computed `ApplicationVersion` must always exceed the shipped Xamarin app's versionCode 34 (the 4.x packed scheme yields ≥ 67108864), or the stores will reject the binary as an upgrade.
- Platform pins, deliberate — don't "fix" them without reading the referenced specs: Android `minSdk 21` / `targetSdk 36` (spec 009; bumped from the Xamarin-era 33 to match the .NET 10 build SDK and Google Play's target-API requirement for updates; exact-alarm permissions intentionally *not* requested), iOS minimum 15.0 (spec 010).
- Signing/publishing credentials live in GitHub Actions **secrets** (`ANDROID_KEYSTORE_BASE64`, `ANDROID_KEYSTORE_PASSWORD`, `ANDROID_KEY_ALIAS`, `ANDROID_KEY_PASSWORD`, `GOOGLE_PLAY_SERVICE_ACCOUNT_JSON`, `APPLE_CERT_P12_BASE64`, `APPLE_CERT_P12_PASSWORD`, `APPSTORE_ISSUER_ID`, `APPSTORE_KEY_ID`, `APPSTORE_PRIVATE_KEY`) and **variables** (`APPLE_CODESIGN_KEY` — the distribution cert common name, `APPLE_PROFILE_NAME` — the App Store provisioning profile name). No secrets are committed to the repo.

## Security considerations

- No credentials, keys, or secrets live in the repository; signing material comes from GitHub Actions secrets.
- The SQLite database is opened read-only and ships as an embedded resource — it contains reflection text, not user data.
- User data (sober date, preferences) lives only in platform-local settings (`ApplicationData.LocalSettings`, mirrored to Android SharedPreferences). `SettingsService` handles legacy `DateTime` encodings during import — preserve that tolerance.
- The legacy-settings migration path (`StartupMigrationRunner`, `SettingsService.MigrateOldPreferences`, `PreferenceConstants`/`VersionConstants`) is the trust boundary between old installs and the new app; changes there can break upgrades for existing users and need tests.
- **SQLite vulnerability (NU1903) is fixed**: `SQLitePCLRaw.bundle_e_sqlite3` 3.0.4 + `SQLitePCLRaw.lib.e_sqlite3` 3.50.3 are pinned both in the head (`DailyReflection/Directory.Packages.props`) and in `DailyReflection.Data`, lifting the whole graph above the vulnerable `lib.e_sqlite3` ≤ 2.1.11 range ([GHSA-2m69-gcr7-jv3q](https://github.com/advisories/GHSA-2m69-gcr7-jv3q)). `lib.e_sqlite3` 3.50.x is a shim over `SourceGear.sqlite3`, which ships the patched native SQLite; the head additionally pins `lib.e_sqlite3.android`/`.ios` to 2.1.12 with `ExcludeAssets="all"` so the vulnerable transitive copy from `sqlite-net-pcl`'s `bundle_green` contributes no files. Keep the pins in sync when upgrading.

## Documentation map

- `README.md` — store links and the Uno-port upgrade/migration summary.
- `docs/ANALYSIS.md` — deep gap analysis of the Uno port vs. the Xamarin original (§10 enumerates every gap; some early sections describe MAUI/Avalonia heads that are not present on this branch).
- `specs/` — 11 implementation specs (001–011, all marked Implemented) that closed those gaps, plus post-parity feature specs 012 (app theme preference), 013 (Settings picker rows as card-look ComboBoxes) and 014 (FlipView paging of the readings, in progress); each has acceptance criteria and a "done when" checklist. When a code comment cites `Spec NNN §X`, look here.
- `prompt.md` — the original migration brief; source of the "no visual redesign" and "Xamarin behaviour is the source of truth" constraints.
- `CLAUDE.md` — a pointer back to this file, kept so Claude Code and other CLAUDE.md-aware agents land here. Keep agent guidance in this file only.
