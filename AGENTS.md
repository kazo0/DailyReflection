# AGENTS.md

Guidance for AI coding agents working in this repository. Read this first; it assumes no prior knowledge of the project.

## Project overview

**Daily Reflection** is a cross-platform mobile/desktop app that shows daily excerpts from a book of reflections by A.A. members, tracks the user's sobriety time, and schedules a daily reminder notification. It is published on the Apple App Store and Google Play (`com.kazo0.dailyreflection`).

This branch contains the **Uno Platform port** of the original Xamarin.Forms app ([kazo0/DailyReflection](https://github.com/kazo0/DailyReflection)). The port is designed to upgrade the Xamarin app **in place** on the stores: it keeps the original `ApplicationId` (`com.kazo0.dailyreflection`) and bumps the version to 4.0 (35), above the Xamarin app's 3.4 (34). On first launch after the upgrade, version-gated startup migrations import user settings (sober date, notification time/enabled, display preference) from the legacy platform stores (Android SharedPreferences, iOS `DR_Settings` NSUserDefaults suite) and re-schedule the daily notification.

The UX is three tabs: **Reflection** (daily reading, date picker, share), **Sober Time** (years/months/days since sober date), **Settings** (sober date, notification time, display preference). A hard requirement of the port (see `prompt.md`) is *no visual redesign* — UI work restores the original Xamarin Shell behaviour, it does not refresh it.

## Technology stack

- **.NET 10** — shared libraries target `net10.0`; the app head targets `net10.0-android`, `net10.0-ios`, `net10.0-desktop`.
- **Uno Platform 6.x** single project (`Uno.Sdk` **6.5.31**, pinned in `global.json`; `allowPrerelease: false`). Enabled `UnoFeatures`: `SkiaRenderer`, `Hosting`, `Toolkit`, `Configuration`, `Navigation`.
- **WinUI 3 XAML** rendered by the Uno Skia renderer; **Uno.Toolkit `TabBar`** for the tab chrome.
- **Uno.Extensions** — generic host (`Microsoft.Extensions.Hosting`), region-based Navigation (Visibility navigator), Configuration (embedded `appsettings.json`).
- **CommunityToolkit.Mvvm 8.4.0** — MVVM via source generators (`[ObservableProperty]`, `[RelayCommand]`); `WeakReferenceMessenger` for cross-ViewModel messages.
- **sqlite-net-pcl 1.10.196-beta + SQLitePCLRaw.bundle_e_sqlite3** — read-only embedded SQLite database (`dailyreflections.db`) extracted to `LocalApplicationData` on first run.
- **NodaTime 3.0.3** — sober-period arithmetic (`Period.Between`).
- **NUnit 4 + Moq** for unit tests.
- `.mcp.json` configures the Uno App dev-server MCP (`uno.devserver`) and the Uno Platform docs MCP for IDE agents.

## Solution layout

`DailyReflection.slnx` is the solution (XML format) with 7 projects. `DailyReflection-uno.slnf` is a solution filter containing only the head + 4 shared libraries (no test projects).

```
DailyReflection.Core           Constants (AutomationConstants, PreferenceConstants, VersionConstants,
                               ConfigurationConstants) + extensions (ServiceCollectionExtensions with the
                               AddAllSubclassesOf<T> DI helper, HtmlInlineParser, StringExtensions). No project deps.
DailyReflection.Data           Reflection model, SoberTimeDisplayPreference enum, IDailyReflectionDatabase /
                               DailyReflectionDatabase (SQLite). dailyreflections.db is an EmbeddedResource.
DailyReflection.Services       Service interfaces (ISettingsService, IShareService, INotificationService,
                               IVersionTrackingService), IDailyReflectionService implementation, and
                               StartupMigrationRunner (version-gated settings import + DB refresh).
DailyReflection.Presentation   ViewModels (ViewModelBase + DailyReflectionViewModel, SobrietyTimeViewModel,
                               SettingsViewModel), messenger Messages, and the AddPresentationDependencies DI entry.
DailyReflection                The Uno head (DailyReflection.Uno.csproj). Entry point App.OnLaunched().
  ├─ Views/                    MainPage (TabBar shell) + DailyReflectionPage / SobrietyTimePage / SettingsPage.
  ├─ PlatformServices/         Partial-class implementations of the Services interfaces, split per platform by
  │                            filename suffix: .Android.cs / .iOS.cs / .Windows.cs / .Desktop.cs.
  ├─ Platforms/                Per-platform entry points and manifests (AndroidManifest.xml, Info.plist,
  │                            Android BroadcastReceivers for the daily alarm).
  ├─ Converters/               IValueConverter implementations used by the XAML.
  ├─ Styles/                   Colors.xaml / Styles.xaml — theme-aware DR* brushes (DRTabBarBackgroundBrush etc.).
  ├─ Strings/en, Assets/       Localization resources and image assets.
  └─ appsettings.json          Embedded config; the required key is DatabaseFileName (App fails fast if missing).
DailyReflection.Presentation.Tests   NUnit + Moq tests of the ViewModels and messages (net10.0).
DailyReflection.Services.Tests       NUnit + Moq tests of services, startup migrations, HTML parser, plus
                                     lint-style tests (automation-ID coverage, XAML surface shape).
DailyReflection.UITests              LEGACY .NET Framework 4.8 Xamarin.UITest scaffold. NOT in the solution;
                                     its csproj references a Xamarin-era project path that no longer exists.
                                     Treat it as read-only reference for AutomationId-based UI testing.
```

### Architecture rules worth knowing

- **The four shared libraries are platform-agnostic.** There are no `#if __ANDROID__` / `#if __IOS__` blocks in Core/Data/Services/Presentation. All platform variation lives in the head, in `PlatformServices/` partial classes and `Platforms/`. Keep it that way.
- **DI registration chain** (all via `Microsoft.Extensions.DependencyInjection` extension methods):
  `Platform.AddPlatformServices()` (head: settings/share/notification/version-tracking) → `Presentation.AddPresentationDependencies()` → `AddAllSubclassesOf<ViewModelBase>` (Singleton, reflection scan) → `Services.AddServiceDependencies()` (`IDailyReflectionService` Transient) → `Data.AddDataDependencies()` (`IDailyReflectionDatabase` Singleton).
- **Navigation**: routes are registered in `App.RegisterRoutes` — `Main` (default) with nested routes `Reflection` (default), `SoberTime`, `Settings`. `MainPage` hosts a `TabBar` whose items map to region names; the content region uses the Visibility navigator (tabs are loaded lazily and toggled, not frame-navigated). Pages are registered Transient, ViewModels are Singletons — deliberate, see the comment in `App.xaml.cs`.
- **Cross-ViewModel communication** uses `WeakReferenceMessenger` with typed messages in `DailyReflection.Presentation/Messages/` (`SoberDateChangedMessage`, `SoberTimeDisplayPreferenceChangedMessage`, `NotificationPermissionRequestMessage`).
- **ViewModels** extend `ViewModelBase : ObservableRecipient` and use CommunityToolkit.Mvvm source generators — never hand-write `INotifyPropertyChanged` plumbing.
- **Startup migrations** (`StartupMigrationRunner`) run after first paint and are version-gated by `VersionConstants`; they port the Xamarin `App.OnStart` behaviour. Be careful here — they touch the store-upgrade path for real users.
- Code comments reference design docs by spec number and section (e.g. `Spec 004 §D`) — see `specs/`.

## Build and test commands

Requires the **.NET 10 SDK** (10.0.110 verified working). Building the Android/iOS TFMs requires the corresponding .NET mobile workloads; the desktop TFM and the test projects do not.

```bash
# Build everything in the solution (needs Android + iOS workloads installed)
dotnet build DailyReflection.slnx

# Build only the Uno head + shared libraries (solution filter)
dotnet build DailyReflection-uno.slnf

# Build the head for desktop only — no mobile workloads needed, fastest check
# (verified: builds clean, 0 errors, ~25 s on .NET SDK 10.0.110)
dotnet build DailyReflection/DailyReflection.Uno.csproj -f net10.0-desktop

# Run the app on desktop
dotnet run --project DailyReflection/DailyReflection.Uno.csproj -f net10.0-desktop

# Unit tests (NUnit) — verified green: 23 presentation + 27 services = 50 tests
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

- Unit tests are NUnit 4 + Moq on `net10.0`, split by layer: `DailyReflection.Presentation.Tests` (ViewModel behaviour, message propagation, share-closure invariant; base class `ViewModelTestBase`) and `DailyReflection.Services.Tests` (service plumbing, startup-migration version gates, HTML inline parser; base class `ServiceTestBase`).
- The Services tests include repo-level lint tests: `AutomationConstantsCoverageTests` (every automation-ID constant is used in at least one XAML view) and `ViewSurfaceTests` (XAML binding contract / z-order / theme-brush assertions). When you change XAML structure or automation IDs, run these.
- `DailyReflection.UITests` is a legacy Xamarin.UITest (.NET Framework 4.8) scaffold that is **not buildable** in the current tree (not in the solution; references a removed Xamarin project). Use it only as a reference for how view-level UI tests were structured (page-object pattern keyed on AutomationIds).
- All 50 unit tests pass on .NET SDK 10.0.110 as of this writing; keep them green.

## Deployment / CI

- `azure-pipelines.yml` is a **legacy Xamarin-era pipeline** (XamarinAndroid/XamariniOS tasks, `.sln` restore). It does not build the Uno head and is stale — do not treat it as the current release process; update or replace it if you set up CI for the Uno port.
- **Store identity is load-bearing** (see `DailyReflection/DailyReflection.Uno.csproj`): `ApplicationId` must stay `com.kazo0.dailyreflection` and `ApplicationDisplayVersion`/`ApplicationVersion` (4.0 / 35) must always exceed the shipped Xamarin app's 3.4 (34), or the stores will reject the binary as an upgrade.
- Platform pins, deliberate — don't "fix" them without reading the referenced specs: Android `minSdk 21` / `targetSdk 33` (spec 009; exact-alarm permissions intentionally *not* requested), iOS minimum 15.0 (spec 010).
- Android release signing uses a keystore stored as an Azure DevOps secure file (pipeline variables `KEYSTORE-PASS`, `KEYSTORE-ALIAS`, `KEY-PASS`). No secrets are committed to the repo.

## Security considerations

- No credentials, keys, or secrets live in the repository; signing material comes from CI secure files.
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
