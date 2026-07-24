# AGENTS.md

Guidance for AI coding agents working in this repository. Read this first; it assumes no prior knowledge of the project.

## Project overview

**Daily Reflection** is a cross-platform mobile/desktop app that shows daily excerpts from a book of reflections by A.A. members, tracks the user's sobriety time, and schedules a daily reminder notification. It is published on the Apple App Store and Google Play (`com.kazo0.dailyreflection`).

This branch contains the **Uno Platform port** of the original Xamarin.Forms app ([kazo0/DailyReflection](https://github.com/kazo0/DailyReflection)). The port is designed to upgrade the Xamarin app **in place** on the stores: it keeps the original `ApplicationId` (`com.kazo0.dailyreflection`) and moves to 4.x versions computed by Nerdbank.GitVersioning (packed Android versionCode ≥ 67108864, above the Xamarin app's 3.4 (34)). On first launch after the upgrade, version-gated startup migrations import user settings (sober date, notification time/enabled, display preference) from the legacy platform stores (Android SharedPreferences, iOS `DR_Settings` NSUserDefaults suite) and re-schedule the daily notification.

The UX is three tabs: **Reflection** (daily reading, date picker, share), **Sober Time** (years/months/days since sober date), **Settings** (sober date, notification time, display preference). A hard requirement of the port (see `prompt.md`) is *no visual redesign* — UI work restores the original Xamarin Shell behaviour, it does not refresh it.

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
DailyReflection.Data           Reflection model, SoberTimeDisplayPreference enum, IDailyReflectionDatabase /
                               DailyReflectionDatabase (SQLite). dailyreflections.db is an EmbeddedResource.
DailyReflection.Services       Service interfaces (ISettingsService, IShareService, INotificationService,
                               IVersionTrackingService), IDailyReflectionService implementation, and
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
  ├─ Styles/                   Colors.xaml / Styles.xaml — theme-aware DR* brushes (DRTabBarBackgroundBrush etc.)
  │                            + ColorPaletteOverride.xaml (DR palette → Uno Material color keys).
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
  `Platform.AddPlatformServices()` (head: settings/share/notification/version-tracking) → `Presentation.AddPresentationDependencies()` (the three MVUX models **and** the generated `Bindable*Model` view-models that wrap them, all Singleton — registering the bindables matters: the navigator resolves view models from DI first, and its fallback construction would new up a second, disconnected instance of the model) → `Services.AddServiceDependencies()` (`IDailyReflectionService` Transient) → `Data.AddDataDependencies()` (`IDailyReflectionDatabase` Singleton).
- **Navigation**: routes are registered in `App.RegisterRoutes` — `Main` (default) with nested routes `Reflection` (default), `SoberTime`, `Settings`. `MainPage` hosts a `TabBar` whose items map to region names; the content region uses the Visibility navigator (tabs are loaded lazily and toggled, not frame-navigated). `OnLaunched` uses the MVUX overload `UseNavigation(ReactiveViewModelMappings.ViewModelMappings, RegisterRoutes)` so the navigator wraps the DI-resolved model in its generated `Bindable*Model` and sets that as the page's DataContext — pages never set DataContext themselves. Pages are registered Transient, models/bindables Singleton — deliberate, see the comments in `App.xaml.cs` and `AddPresentationDependencies`.
- **Cross-model sync is feed composition, not a messenger.** `SobrietyTimeModel` takes the singleton `SettingsModel` and projects its states (`SoberDate`, `SoberTimeDisplayPreference`) into flat feeds via `Select` — a Settings edit re-derives the Sober Time tab automatically. There is no `WeakReferenceMessenger`/`Messages/` infrastructure anymore.
- **MVUX models** are `partial record`s exposing `IFeed<T>` (read-only) and `IState<T>` (two-way-bound). Side effects (persistence, notification scheduling) run as `ForEach` callbacks that compare the new value against the last-persisted one so the subscription's initial replay is a no-op — startup must stay side-effect free (`StartupMigrationRunner` owns startup re-scheduling). Public `ValueTask` methods on a model become generated commands bound from XAML (`{Binding Share}`). Classic `{Binding}` is used in the views (the navigator owns DataContext); a bindable's feed unwraps at the leaf of the path only, so models expose flat feeds rather than nested record paths.
- **Startup migrations** (`StartupMigrationRunner`) run after first paint and are version-gated by `VersionConstants`; they port the Xamarin `App.OnStart` behaviour. Be careful here — they touch the store-upgrade path for real users.
- Code comments reference design docs by spec number and section (e.g. `Spec 004 §D`) — see `specs/`.

## Build and test commands

Requires the **.NET 10 SDK** (10.0.110 verified working). Building the Android/iOS TFMs requires the corresponding .NET mobile workloads; the desktop TFM and the test projects do not.

```bash
# Build everything in the solution (needs Android + iOS workloads installed)
dotnet build DailyReflection.slnx

# Build the head for desktop only — no mobile workloads needed, fastest check
# (verified: builds clean, 0 errors, ~25 s on .NET SDK 10.0.110)
dotnet build DailyReflection/DailyReflection.Uno.csproj -f net10.0-desktop

# Run the app on desktop
dotnet run --project DailyReflection/DailyReflection.Uno.csproj -f net10.0-desktop

# Unit tests (NUnit) — verified green: 24 presentation + 28 services = 52 tests
dotnet test DailyReflection.Presentation.Tests/DailyReflection.Presentation.Tests.csproj
dotnet test DailyReflection.Services.Tests/DailyReflection.Services.Tests.csproj

# Run a single test
dotnet test DailyReflection.Presentation.Tests/DailyReflection.Presentation.Tests.csproj --filter "FullyQualifiedName~TestMethodName"
```

The Uno.Sdk version comes from `global.json` — update it there, not in the csproj. The head uses **central package management** (`ManagePackageVersionsCentrally` in `DailyReflection/Directory.Build.props`, versions in `DailyReflection/Directory.Packages.props`); the shared libraries pin package versions inline in their own csproj files.

## Code style guidelines

There is **no `.editorconfig`** in this repo (older docs claim otherwise — that is stale). The observed conventions, which you should match per-file rather than restyle:

- **CRLF line endings everywhere.** `.gitattributes` enforces this; do not convert files to LF.
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
- All 52 unit tests pass on .NET SDK 10.0.110 as of this writing; keep them green.

## Deployment / CI

- CI/CD is **GitHub Actions**:
  - `.github/workflows/ci.yml` — the merge gate for PRs and `master`: unit tests, desktop build, unsigned Android build, iOS simulator build. These four jobs are intended to be required status checks on `master`.
  - `.github/workflows/release.yml` — triggered by any push to a `release/*` branch: computes/validates the version, runs tests, builds a signed `.aab`/`.apk`, a signed `.ipa`, and self-contained desktop zips (win-x64 / linux-x64 / osx-arm64), then **waits for manual approval** on the `production` GitHub Environment before uploading to Google Play, uploading + submitting to App Store Connect (fastlane `deliver`), and creating a GitHub release — which pushes the `vX.Y.Z` tag. `workflow_dispatch` inputs allow dry runs (Play test track, skip App Store review submission).
- **Versioning is Nerdbank.GitVersioning** (`version.json` at the repo root; master carries `X.Y-alpha`). Cut release branches with `nbgv prepare-release` (creates `release/vX.Y` with the stable version and bumps master to the next `-alpha`). NBGV's built-in mobile targets (`NBGV_SetVersionForMauiAndroid`/`IOS`) set the store versions: Android versionCode = `major<<24 | minor<<16 | git height` and versionName = the semantic version; iOS uses the three-part version for `CFBundleVersion`/`CFBundleShortVersionString`. Do not hardcode `ApplicationVersion`/`ApplicationDisplayVersion` in the csproj, and never switch to a scheme that produces smaller versionCodes once a release has shipped.
- **Store identity is load-bearing**: `ApplicationId` must stay `com.kazo0.dailyreflection` and the computed `ApplicationVersion` must always exceed the shipped Xamarin app's versionCode 34 (the 4.x packed scheme yields ≥ 67108864), or the stores will reject the binary as an upgrade.
- Platform pins, deliberate — don't "fix" them without reading the referenced specs: Android `minSdk 21` / `targetSdk 33` (spec 009; exact-alarm permissions intentionally *not* requested), iOS minimum 15.0 (spec 010). **Known release blocker:** Google Play now requires app updates to target API 35+, so the `targetSdk 33` pin must be revisited (with a spec-009 review) before a Play release can go out.
- Signing/publishing credentials live in GitHub Actions **secrets** (`ANDROID_KEYSTORE_BASE64`, `ANDROID_KEYSTORE_PASSWORD`, `ANDROID_KEY_ALIAS`, `ANDROID_KEY_PASSWORD`, `GOOGLE_PLAY_SERVICE_ACCOUNT_JSON`, `APPLE_CERT_P12_BASE64`, `APPLE_CERT_P12_PASSWORD`, `APPSTORE_ISSUER_ID`, `APPSTORE_KEY_ID`, `APPSTORE_PRIVATE_KEY`) and **variables** (`APPLE_CODESIGN_KEY` — the distribution cert common name, `APPLE_PROFILE_NAME` — the App Store provisioning profile name). No secrets are committed to the repo.

## Security considerations

- No credentials, keys, or secrets live in the repository; signing material comes from GitHub Actions secrets.
- The SQLite database is opened read-only and ships as an embedded resource — it contains reflection text, not user data.
- User data (sober date, preferences) lives only in platform-local settings (`ApplicationData.LocalSettings`, mirrored to Android SharedPreferences). `SettingsService` handles legacy `DateTime` encodings during import — preserve that tolerance.
- The legacy-settings migration path (`StartupMigrationRunner`, `SettingsService.MigrateOldPreferences`, `PreferenceConstants`/`VersionConstants`) is the trust boundary between old installs and the new app; changes there can break upgrades for existing users and need tests.
- **Known vulnerability warning**: building the desktop TFM reports NU1903 — the transitive `SQLitePCLRaw.lib.e_sqlite3` **2.1.2** (pulled in via `sqlite-net-pcl` in the shared libraries, which do not use central package management) has a known high-severity vulnerability ([GHSA-2m69-gcr7-jv3q](https://github.com/advisories/GHSA-2m69-gcr7-jv3q)). The head pins `SQLitePCLRaw.bundle_e_sqlite3` 2.1.10 in `DailyReflection/Directory.Packages.props`, but the shared projects resolve the older transitive version. Fixing this (e.g. an explicit pin in `DailyReflection.Data`) is worthwhile follow-up.

## Documentation map

- `README.md` — store links and the Uno-port upgrade/migration summary.
- `docs/ANALYSIS.md` — deep gap analysis of the Uno port vs. the Xamarin original (§10 enumerates every gap; some early sections describe MAUI/Avalonia heads that are not present on this branch).
- `specs/` — 11 implementation specs (001–011, all marked Implemented) that closed those gaps, each with acceptance criteria and a "done when" checklist. When a code comment cites `Spec NNN §X`, look here.
- `prompt.md` — the original migration brief; source of the "no visual redesign" and "Xamarin behaviour is the source of truth" constraints.
- `CLAUDE.md` — a pointer back to this file, kept so Claude Code and other CLAUDE.md-aware agents land here. Keep agent guidance in this file only.
