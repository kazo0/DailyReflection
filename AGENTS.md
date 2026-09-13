# AGENTS.md

Guidance for AI coding agents working in this repository. Read this first; it assumes no prior knowledge of the project.

## Hard rules

**Never use the owner's admin rights to bypass branch protection.** This is not a
default to weigh against convenience — an agent may not do it on its own under any
circumstance, including when the change is small, urgent, obviously correct, or
fixing something the agent itself broke.

`master` is protected by a **ruleset** (pull request required, 1 approving review,
5 required status checks — the classic branch-protection API 404s for this repo,
which is expected), but repository-admin **bypass is enabled**. That means a plain
`git push origin master` as the owner *succeeds* and GitHub reports `Bypassed rule
violations` after the fact, and `gh pr merge --admin` lands a PR that has no
approval. There is no `--force` involved and no prompt — the guardrail simply does
not apply to this account. Treat those as forbidden, not as permitted-because-
they-worked.

A second ruleset, `release branches`, blocks deletion and force-push on
`refs/heads/release/**` and has **no bypass actors**, so it does apply to the
owner. Ordinary pushes to a release branch (the hotfix flow) are unaffected.

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

The UX is three tabs: **Reflection** (daily reading, date picker, share), **Sober Time** (years/months/days since sober date), **Settings** (sober date, notification time, display preference). A hard requirement of the port (see `prompt.md`) is *no visual redesign* — UI work restores the original Xamarin Shell behaviour, it does not refresh it. The one sanctioned departure is the **responsive shell**: on windows ≥ 700px the tabs are presented as a vertical `TabBar` rail on the left instead of the bottom `TabBar` (the Xamarin app was phone-only, so there is no original behaviour to restore for desktop). Narrow windows still get the bottom TabBar exactly as before.

## Technology stack

- **.NET 10** — shared libraries target `net10.0`; the app head targets `net10.0-android`, `net10.0-ios`, `net10.0-desktop`.
- **Uno Platform 6.x** single project (`Uno.Sdk` pinned in `global.json`; `allowPrerelease: false`). Enabled `UnoFeatures`: `SkiaRenderer`, `Hosting`, `Toolkit`, `Material`, `Configuration`, `Navigation`, `Mvux`.
- **WinUI 3 XAML** rendered by the Uno Skia renderer; **Uno Material** theme (`MaterialToolkitTheme` in `App.xaml` with `Styles/ColorPaletteOverride.xaml` mapping the Xamarin-era DR palette onto Material color keys — primary stays `#1976D2`); **Uno.Toolkit `TabBar`** for the tab chrome — bottom bar on narrow windows, vertical rail (`VerticalTabBarStyle`) on wide ones, switched by the Toolkit's `{utu:Responsive}` markup extension — and **Uno.Toolkit `NavigationBar`** as the top app bar on every page.
- **Uno.Extensions** — generic host (`Microsoft.Extensions.Hosting`), region-based Navigation (Visibility navigator), Configuration (embedded `appsettings.json`).
- **MVUX (Uno.Extensions.Reactive 7.1.1)** — presentation is `partial record` models with `IFeed`/`IState` + generated `{Name}ViewModel` view-models (bindable generation tool **v3**, pinned by `[assembly: BindableGenerationTool(3)]` in `DailyReflection.Presentation/AssemblyInfo.cs` and the head's `AssemblyInfo.cs`; v2 named them `Bindable{Name}Model`); `Uno.Extensions.Reactive.Messaging` 7.1.1 uses the CommunityToolkit messenger for reading-preference updates; no hand-written `INotifyPropertyChanged`.
- **sqlite-net-pcl 1.10.196-beta + SQLitePCLRaw.bundle_e_sqlite3** — read-only embedded SQLite database (`dailyreflections.db`) extracted to `LocalApplicationData` on first run.
- **NodaTime 3.0.3** — sober-period arithmetic (`Period.Between`).
- **NUnit 4 + Moq** for unit tests.
- `.mcp.json` configures the Uno App dev-server MCP (`uno.devserver`) and the Uno Platform docs MCP for IDE agents.

## Solution layout

`DailyReflection.slnx` is the solution (XML format) with 7 projects.

```
DailyReflection.Core           Entities (the immutable Reflection record — see the MVUX note below for why it
                               lives here) + Constants (AutomationConstants, PreferenceConstants, VersionConstants,
                               ConfigurationConstants) + extensions (ServiceCollectionExtensions with the
                               AddAllSubclassesOf<T> DI helper, HtmlInlineParser, StringExtensions). No project deps.
DailyReflection.Data           ReflectionDto (sqlite row), SoberTimeDisplayPreference enum, IDailyReflectionDatabase /
                               DailyReflectionDatabase (SQLite). dailyreflections.db is an EmbeddedResource.
DailyReflection.Services       Service interfaces (ISettingsService, IShareService, IClipboardService, INotificationService,
                               IVersionTrackingService), IDailyReflectionService implementation, and
                               StartupMigrationRunner (version-gated settings import + DB refresh).
DailyReflection.Presentation   MVUX models (partial records: DailyReflectionModel, SobrietyTimeModel,
                               SettingsModel) exposing IFeed/IState; the MVUX generator emits {Name}ViewModel
                               view-models + ReactiveViewModelMappings into this assembly. References
                               Uno.Extensions.Reactive(.WinUI) 7.1.1. AddPresentationDependencies is the DI entry.
DailyReflection                The Uno head (DailyReflection.Uno.csproj). Entry point App.OnLaunched().
  ├─ Views/                    MainPage (responsive bottom/vertical TabBar shell) + DailyReflectionPage /
  │                            SobrietyTimePage / SettingsPage,
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
  `Platform.AddPlatformServices()` (head: settings/share/clipboard/notification/version-tracking) → `Presentation.AddPresentationDependencies()` (the three MVUX models **and** the generated `{Name}ViewModel` view-models that wrap them, all Singleton — registering the bindables matters: the navigator resolves view models from DI first, and its fallback construction would new up a second, disconnected instance of the model) → `Services.AddServiceDependencies()` (`IDailyReflectionService` Transient) → `Data.AddDataDependencies()` (`IDailyReflectionDatabase` Singleton).
- **Navigation**: routes are registered in `App.RegisterRoutes` — `Main` (default) with nested routes `Reflection` (default), `SoberTime`, `Settings`. `MainPage` hosts **two** `TabBar`s — a bottom bar and a vertical rail (`VerticalTabBarStyle`) — each region-attached with the same `Region.Name` values, driving one shared content region on the Visibility navigator (tabs are loaded lazily and toggled, not frame-navigated). Which bar is visible is decided by `{utu:Responsive}` against the page's `ShellResponsiveLayout` (Narrow 0 / Normal 700): bottom bar below 700px, side rail from 700px up. Both stay in the visual tree — a `ResponsiveView` (template swapping) would tear down the region-attached controls on every breakpoint change. There is no code-behind in the shell. A `NavigationView` was tried and rejected: Uno.Material's implicit style is the only one it has (shadowing it with an empty style or `{x:Null}` leaves it template-less and the shell blank), its built-in settings item needs code-behind to get a region name, and its collapsed pane still reserved a column on narrow windows. `ViewSurfaceTests` pins the shape. `OnLaunched` uses the MVUX overload `UseNavigation(ReactiveViewModelMappings.ViewModelMappings, RegisterRoutes)` so the navigator wraps the DI-resolved model in its generated `{Name}ViewModel` and sets that as the page's DataContext — pages never set DataContext themselves. Pages are registered Transient, models/bindables Singleton — deliberate, see the comments in `App.xaml.cs` and `AddPresentationDependencies`.
- **Cross-model sync uses feed composition and MVUX messaging.** `SobrietyTimeModel` takes the singleton `SettingsModel` and projects its states (`SoberDate`, `SoberTimeDisplayPreference`) into flat feeds via `Select` — a Settings edit re-derives the Sober Time tab automatically. `SettingsModel` persists the secular-reading preference and sends an `EntityMessage<ReadingPreference>` through the singleton `IMessenger` (`WeakReferenceMessenger`). `DailyReflectionModel` seeds its own preference state from `ISettingsService` and uses MVUX `Observe` to apply these updates; combining that state with `Date` reloads the selected day. It does not depend on `SettingsModel`.
- **MVUX models** are `partial record`s exposing `IFeed<T>` (read-only) and `IState<T>` (two-way-bound). Side effects (persistence, notification scheduling) run as `ForEach` callbacks that compare the new value against the last-persisted one so the subscription's initial replay is a no-op — startup must stay side-effect free (`StartupMigrationRunner` owns startup re-scheduling). Public `ValueTask` methods on a model become generated commands bound from XAML (`{Binding Share}`). Classic `{Binding}` is used in the views (the navigator owns DataContext); a state unwraps to its value at the leaf of the path, so models expose flat states rather than nested record paths.
- **Feed value types must be records, and `Reflection` lives in `DailyReflection.Core/Entities`.** MVUX only generates a feed-carrying `Bindable<T>` proxy (`BindableReflectionViewModel`) for a feed whose value type is a record; for a mutable POCO the generated view-model property collapses to the *unwrapped value*, and since `FeedView.Source` is typed `object`, `Source="{Binding DailyReflection}"` then binds a non-feed and the page renders blank with no error. The sqlite row stays a POCO (`Data.Models.ReflectionDto`) because sqlite-net needs settable properties; `Services/DailyReflection/ReflectionMapping.cs` maps DTO → record at the service boundary. Keep the record out of `DailyReflection.Services.*`: the generator writes the record's type name unqualified inside its own namespace, so under `DailyReflection.Services.DailyReflection` the leading `DailyReflection` binds to that namespace instead of the root and the generated proxy fails to compile (CS0234). `GeneratedViewModelTests` guards the generated shape; `ViewSurfaceTests` pins the XAML.
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

# Unit tests (NUnit) — verified green: 24 presentation + 28 services = 52 tests
dotnet test DailyReflection.Presentation.Tests/DailyReflection.Presentation.Tests.csproj
dotnet test DailyReflection.Services.Tests/DailyReflection.Services.Tests.csproj

# Run a single test
dotnet test DailyReflection.Presentation.Tests/DailyReflection.Presentation.Tests.csproj --filter "FullyQualifiedName~TestMethodName"
```

### Building a single platform

`DailyReflection.Uno.csproj` is the only crosstargeted project (`net10.0-android;net10.0-ios;net10.0-desktop`); the four shared libraries and the tests are plain `net10.0`. Passing `-f` alone still makes **restore** resolve every TFM, which requires the mobile workloads even for a desktop-only build. `TargetFrameworkOverride` narrows the project itself, so restore and build only ever see the platforms you asked for:

```bash
# Platform suffixes: android, ios, desktop. Semicolon-separated for more than one.
dotnet build DailyReflection/DailyReflection.Uno.csproj -c Release -p:TargetFrameworkOverride=desktop
dotnet build DailyReflection/DailyReflection.Uno.csproj -c Release "-p:TargetFrameworkOverride=android;desktop"
```

The csproj expands each suffix to its versioned TFM; unset (the default) means all three. It is also read from the environment, which is how each CI job pins itself to one platform (`env: TargetFrameworkOverride: desktop` in `.github/workflows/*.yml`).

For local/IDE builds, copy `crosstargeting_override.props.sample` (repo root) → `crosstargeting_override.props` and uncomment the platform you want; the file is git-ignored and imported by `DailyReflection/Directory.Build.props`. **Close the IDE before changing it** — switching platforms while the solution is open corrupts the NuGet restore cache.

The Uno.Sdk version comes from `global.json` — update it there, not in the csproj. The head uses **central package management** (`ManagePackageVersionsCentrally` in `DailyReflection/Directory.Build.props`, versions in `DailyReflection/Directory.Packages.props`); the shared libraries pin package versions inline in their own csproj files.

### Native AOT publish (what the stores get)

The Android and iOS store packages are **Native AOT** (Uno Platform 6.6+ feature, <https://platform.uno/docs/articles/features/native-aot.html>). The `PublishAot` block in `DailyReflection.Uno.csproj` switches it on only for `dotnet publish` (the CLI's `_IsPublishing` property) on the `-android` / `-ios` TFMs — a plain `dotnet build`, Debug deploys, and hot reload keep the regular Mono/CoreCLR runtime, and desktop is excluded (Native AOT cannot cross-compile; the desktop zips are published for three RIDs from one Linux runner). Reproduce the release builds locally with:

```bash
# iOS device build (what release.yml signs). EnableCodeSigning=false skips the
# signing step so no certificate/profile is needed just to exercise the compiler.
dotnet publish DailyReflection/DailyReflection.Uno.csproj -f net10.0-ios -c Release -r ios-arm64 -p:TargetFrameworkOverride=ios -p:EnableCodeSigning=false

# iOS simulator — same toolchain, and the .app can be run in a simulator
# (xcrun simctl install booted DailyReflection/bin/Release/net10.0-ios/iossimulator-arm64/DailyReflection.Uno.app).
# The iOS SDK refuses 'dotnet publish' for simulator RIDs, so this uses 'build' and
# sets the SDK's own publish gate (_IsPublishing) by hand — local smoke tests only.
dotnet build DailyReflection/DailyReflection.Uno.csproj -f net10.0-ios -c Release -r iossimulator-arm64 -p:TargetFrameworkOverride=ios -p:_IsPublishing=true

# Android — needs NDK r27+ (AndroidNdkDirectory, or an ndk/ folder under the Android SDK).
# release.yml builds android-arm64 + android-x64; one RID is enough locally.
dotnet publish DailyReflection/DailyReflection.Uno.csproj -f net10.0-android -c Release -r android-arm64 -p:TargetFrameworkOverride=android

# Opt out for a single invocation (falls back to the runtime's own Mono AOT). Pass this
# switch, never PublishAot itself: as a global property PublishAot also reaches the
# net10.0 class libraries, which fail with NETSDK1203 for a mobile RuntimeIdentifier.
#   -p:PublishNativeAot=false
# Uno's AOT diagnostics (trimmer warnings in full, reflection metadata + .mstat dumps):
#   -p:TrimmerSingleWarn=false -p:_ExtraTrimmerArgs=--verbose -p:IlcGenerateMetadataLog=true -p:IlcGenerateMstatFile=true
```

Every project sets `IsAotCompatible`, so the trim / AOT / single-file analyzers run on every build (including the fast desktop one). Treat new `IL2xxx` / `IL3xxx` warnings as bugs: fix them with `[DynamicallyAccessedMembers]`, `[DynamicDependency]`, or a generic API overload — do not `NoWarn` them. The `UseNavigation` / `NavigateAsync` calls in `App.xaml.cs` raise `IL2026` from Uno.Extensions Navigation's own `RequiresUnreferencedCode` annotations; `App.OnLaunched` carries a targeted `[UnconditionalSuppressMessage]` for them (propagating `RequiresUnreferencedCode` to the override instead is `IL2046` — `Application.OnLaunched` is not annotated). That suppression is sound only because every type Navigation resolves reflectively is rooted by the `BindableTypeProvidersSourceGenerator`, so keep the paragraph below in mind when binding to a new type.

XAML `{Binding}` paths do not depend on runtime reflection for the types Uno's `BindableTypeProvidersSourceGenerator` sees when the head compiles — today the `Bindable*Model` view-models, the three models, `Reflection`, `SoberTimeDisplayPreference`, and the pages. When you bind to a new type, build with `-p:EmitCompilerGeneratedFiles=true` and confirm it appears in that generator's output; a type it misses needs `[Microsoft.UI.Xaml.Data.Bindable]` (see the Uno Native AOT doc), otherwise the binding silently resolves to nothing on a device.

## Code style guidelines

Formatting is enforced in CI with `--verify-no-changes`: `dotnet format whitespace` + `dotnet format style` against the root `.editorconfig` for C# (tabs, final newline, file-scoped namespaces, usings sorted alphabetically with `System` not first and aliases last), and XamlStyler in passive mode against `Settings.XamlStyler` for XAML. The `Formatting` job covers the shared libraries and the desktop head; because `dotnet format` only sees files compiled for the TFM it loads, the Android and iOS build jobs run the same two commands on the head to cover `*.Android.cs` / `*.iOS.cs` / `Platforms/`. Plain `dotnet format` (which also applies every analyzer's code fixes) is deliberately not used: the Android platform-compat analyzers (CA1416/CA1422) report diagnostics their fixers cannot apply. Run the same checks locally before pushing; drop `--verify-no-changes` / `--passive` to auto-fix:

```bash
dotnet tool restore   # installs xstyler from .config/dotnet-tools.json
dotnet xstyler --passive --recursive --config Settings.XamlStyler --directory DailyReflection
TargetFrameworkOverride=desktop dotnet format whitespace DailyReflection.slnx --verify-no-changes
TargetFrameworkOverride=desktop dotnet format style DailyReflection.slnx --verify-no-changes
# Platform-only sources (needs that platform's workload; same with ios)
TargetFrameworkOverride=android dotnet format whitespace DailyReflection/DailyReflection.Uno.csproj --verify-no-changes
TargetFrameworkOverride=android dotnet format style DailyReflection/DailyReflection.Uno.csproj --verify-no-changes
```

Conventions beyond what the tools check, which you should match per-file rather than restyle:

- **CRLF line endings everywhere.** `.gitattributes` enforces this; do not convert files to LF.
- **Tabs for indentation** in C# and XAML, in every project (the head was converted from 4 spaces; `.editorconfig` and `Settings.XamlStyler` both say tabs).
- `Nullable` is enabled and `LangVersion` is `Latest` in all projects; the head also has `ImplicitUsings` (see its `GlobalUsings.cs`).
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
  - `.github/workflows/ci.yml` — the merge gate for PRs and `master`: formatting (dotnet format + XamlStyler), unit tests, desktop build, unsigned Android build, iOS simulator build. The two mobile jobs have two modes, switched by the workflow-level `RELEASE_BUILD` expression: for PRs into `master` and pushes to `master` they run the fast plain `dotnet build` (Mono/CoreCLR); for builds *for* a `release/*` branch (a PR into one) they instead run the same Native AOT `dotnet publish` release.yml ships — Android arm64 unsigned, iOS device unsigned via `EnableCodeSigning=false` (the iOS SDK refuses `publish` for simulator RIDs). Keep the AOT publish out of the everyday path: it is slow (ILLink + ILCompiler + native link). These five job names are the required status checks on `master`; keep them stable.
  - Every job that builds the head pins itself to one platform with a job-level `env: TargetFrameworkOverride: <android|ios|desktop>` (see "Building a single platform"), so a job only restores the TFM it builds and only needs that platform's workload.
  - `.github/workflows/release.yml` — triggered by any push to a `release/*` branch: computes/validates the version, runs tests, builds a signed `.aab`/`.apk` and a signed `.ipa` (both **Native AOT** — see "Native AOT publish" above; the jobs pass the `PublishNativeAot` switch and point Android at the runner's `ANDROID_NDK_HOME`, r27.3), and self-contained desktop zips (win-x64 / linux-x64 / osx-arm64), then **waits for manual approval** on the `production` GitHub Environment before uploading to Google Play, uploading + submitting to App Store Connect (fastlane `deliver`), and creating a GitHub release — which pushes the `vX.Y.Z` tag. `workflow_dispatch` inputs allow dry runs (Play test track, skip App Store review submission, `native_aot=false` to ship the mobile packages with the runtime's Mono AOT instead).
  - Android Native AOT is flagged **experimental** by the .NET for Android SDK (warning `XA1040` on every publish; Uno documents and ships it). If a release needs to fall back, use the `native_aot` dispatch input rather than editing the csproj.
- **Versioning is Nerdbank.GitVersioning** (`version.json` at the repo root; master carries `X.Y-alpha`). Cut release branches with `nbgv prepare-release` (creates `release/vX.Y` with the stable version and bumps master to the next `-alpha`). NBGV's built-in mobile targets (`NBGV_SetVersionForMauiAndroid`/`IOS`) set the store versions: Android versionCode = `major<<24 | minor<<16 | git height` and versionName = the semantic version; iOS uses the three-part version for `CFBundleVersion`/`CFBundleShortVersionString`. Do not hardcode `ApplicationVersion`/`ApplicationDisplayVersion` in the csproj, and never switch to a scheme that produces smaller versionCodes once a release has shipped. Because both store version numbers derive from the git height, re-running a release on the *same* commit reproduces the same `versionCode` / `CFBundleVersion`, which Play and App Store Connect both reject as an already-used build number — push a commit instead of re-running (see `docs/RELEASE-SETUP.md` §6).
- **Store identity is load-bearing**: `ApplicationId` must stay `com.kazo0.dailyreflection` and the computed `ApplicationVersion` must always exceed the shipped Xamarin app's versionCode 34 (the 4.x packed scheme yields ≥ 67108864), or the stores will reject the binary as an upgrade. On iOS the bundle identity comes **only** from the csproj: `Platforms/iOS/Info.plist` deliberately omits `CFBundleIdentifier` / `CFBundleName` / `CFBundleDisplayName` / `CFBundleShortVersionString` / `CFBundleVersion`, and the .NET iOS SDK fills them from `ApplicationId` / `ApplicationTitle` / `ApplicationDisplayVersion` / `ApplicationVersion`. Do not add them back with `$(...)` placeholders — nothing substitutes those; the bundles shipped a literal `$(ApplicationId)` until this was caught (spec 010 supersession note, 2026-09). Verify with `plutil -p <built .app>/Info.plist`.
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
- `specs/` — 11 implementation specs (001–011, all marked Implemented) that closed those gaps, each with acceptance criteria and a "done when" checklist. When a code comment cites `Spec NNN §X`, look here.
- `prompt.md` — the original migration brief; source of the "no visual redesign" and "Xamarin behaviour is the source of truth" constraints.
- `CLAUDE.md` — a pointer back to this file, kept so Claude Code and other CLAUDE.md-aware agents land here. Keep agent guidance in this file only.
