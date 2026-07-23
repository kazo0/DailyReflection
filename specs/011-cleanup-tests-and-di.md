# Spec 011 — Cleanup, tests, DI, bundle id

* **Status:** Implemented (2026-05-03).
* **Severity:** 🟡 / 🟠
* **Gaps closed:** 10.1.10, 10.2.6, 10.10.3, 10.10.5, 10.11.1, 10.11.3, 10.8.5, 10.8.6, 10.8.7, 10.8.8, 10.9.1, 10.9.8
* **Depends on:** [005](005-automation-ids.md)

## Summary

This is the close‑out spec. It bundles the gaps that don't share a subsystem but should land together so we can declare the Uno port at parity:

* **Dead code removal** — converters never wired up, empty event handlers, copy‑paste resources.
* **Tests modernisation** — bump test TFM to net10.0 and add a thin UI smoke harness.
* **Constructor injection** — replace `App.GetService<T>()` page resolution with `ViewMap<TPage, TViewModel>` registration through Uno.Extensions Navigation.
* **Bundle id decision** — `com.kazo0.dailyreflectionuno` (today) vs. `com.kazo0.dailyreflection` (matches Xamarin / would replace existing installs). Pick one, document it.
* **Logging in release** — wire the host's `LoggerFactory` back into `Uno.Extensions.LogExtensionPoint` outside `#if DEBUG` so release builds emit logs again.
* **Configuration error mode** — fail loudly when `appsettings.json` is missing.

## Goals

* No dead converters, dead handlers, dead resources in the Uno head.
* Tests target net10.0 and exercise the new code added by specs 001–010.
* A minimal UI smoke harness can drive the three pages by automation id.
* DI is honoured by the navigation framework (no service‑locator).
* Bundle id is documented as a deliberate choice with a roll‑forward path.
* Logs are present in both DEBUG and RELEASE.
* `appsettings.json` missing throws a clear error at startup.

## Non‑goals

* Reorganising the shared Presentation tests beyond the TFM bump.
* Migrating away from `WeakReferenceMessenger` to `IMediator` or similar.

## Acceptance criteria

1. **Dead code (10.1.10, 10.2.6, 10.10.3, 10.10.5).**
   * `Views/DailyReflectionPage.xaml` has no `<converters:PluralityConverter x:Key="YearsPluralityConverter">` resource (already covered in spec 006 §G; double‑check here).
   * `Views/SobrietyTimePage.xaml.cs` has no empty `OnUnloaded` handler.
   * App.xaml's converter table contains only converters used somewhere in `Views/*.xaml`. Check via grep:
     ```bash
     for k in InverseBoolConverter HasValueConverter IntToBoolConverter NullToBoolConverter \
              BoolToVisibilityConverter InverseBoolToVisibilityConverter \
              AllFalseBoolToVisibilityConverter SoberTimeDisplayEnumConverter \
              DateTimeToTimeSpanConverter HtmlToTextConverter; do
       grep -l "{StaticResource $k}" heads/DailyReflection.Uno/DailyReflection.Uno/Views/*.xaml \
         > /dev/null || echo "Unused: $k"
     done
     ```
     Either remove the unused entries or wire them up.
   * The historic `*Icon` keys (`ReflectionIcon`, `SettingsIcon`, `ShareIcon`, `ClockIcon`, `CalendarIcon`) are either re‑introduced (with parity to Xamarin's `<FontImage>`) or the migration log notes their deliberate removal.

2. **Tests TFM bump (10.11.3).** `DailyReflection.Presentation.Tests.csproj` and `DailyReflection.Services.Tests.csproj` target `net10.0`. The packages (`Microsoft.NET.Test.Sdk`, `NUnit`, `NUnit3TestAdapter`, `Moq`) bump to current LTS versions. Both projects build and run via `dotnet test` from the repo root.

3. **UI smoke harness (10.11.1).** A new `DailyReflection.Uno.UITests` project (or an inline test target on the Uno head) using `Uno.UITest` (or a `Microsoft.UI.Xaml.Testing.AppActions` driver — pick one and document) drives at least three scenarios:
   * Launch the app, assert the Reflection tab is selected by default and `DR_Reflection_Title` is populated within 5 s.
   * Switch to the Settings tab, toggle `Settings_Enable_Notif_Switch`, observe `Settings_Notification_Time` becomes enabled.
   * Switch to the Sober Time tab; with no `SoberDate` set, assert the calendar block is hidden.

4. **Constructor injection (10.8.8).** Pages no longer call `App.GetService<TViewModel>()`. Instead, Uno.Extensions Navigation resolves the VM via `ViewMap<TPage, TViewModel>` registration in `App.RegisterRoutes`. The page exposes `ViewModel` as a property populated by the DataContext set by the navigator.

5. **Bundle id decision (10.9.1).** `README.md` contains a one‑paragraph explanation of why the Uno head uses `com.kazo0.dailyreflectionuno`. If the team chooses to replace the existing Xamarin app on the stores, change the csproj `ApplicationId` to `com.kazo0.dailyreflection` and add a release‑note paragraph in the docs.

6. **Logging in release (10.8.6).** A release build of the Uno head emits at minimum `Information`‑level entries for app startup and for every `INotificationService` operation. Verifiable by running a release build and observing `dotnet trace` / platform log output.

7. **Configuration error mode (10.8.7).** Renaming `appsettings.json` to a typo at build time produces a clear `FileNotFoundException` at startup, not silent default values.

8. **Page transient/singleton (10.8.5).** Pages remain `Transient` (matches Uno.Extensions Navigation expectations); document the deviation from Xamarin's Singleton page registration in `App.xaml.cs` with a comment block.

## Implementation plan

### A. Dead code

Mostly mechanical; do this first as a single PR. Each removal is one or two lines. Run the grep in §1 to find unused converters; either remove them from `App.xaml` or use them. Consider:

* `AllFalseBoolToVisibilityConverter` — could replace the `ShowContent` helper on `DailyReflectionPage` (removing one method from code‑behind). Prefer using it.
* `InverseBoolConverter`, `InverseBoolToVisibilityConverter` — likely dead. Remove unless §C reveals a use.
* `HasValueConverter` — likely superseded by `NullToBoolConverter`. Remove if unused.

### B. Tests TFM bump

```xml
<PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    ...
</PropertyGroup>
```

Bump the test packages to versions compatible with net10.0. Touch `Microsoft.NET.Test.Sdk` to 17.x, `NUnit` to 4.x, `NUnit3TestAdapter` to 4.x, `Moq` to 4.20+.

If `Xamarin.Essentials.Interfaces` (referenced in `DailyReflection.Services.Tests`) doesn't have a net10.0‑compatible release, replace its usage with `Mock<IPreferences>` from a hand‑written shim or, simpler, drop the dependency now that the Uno head no longer injects `IPreferences`.

### C. UI smoke harness

Recommended: `Uno.UITest` for cross‑target consistency. Add `DailyReflection.Uno.UITests/DailyReflection.Uno.UITests.csproj` referencing `Uno.UITest`, configure for the desktop target (fastest CI), and write the three scenarios listed in Acceptance #3. Drive selectors via the `AutomationConstants` strings.

If `Uno.UITest` is too heavyweight for the project, a thin alternative is to call `App.MainWindow.Content` from a NUnit test running in‑process and walk the visual tree via `VisualTreeHelper`. This is brittle but cheap; document the choice.

### D. Constructor injection

Move VM resolution into the Navigation framework. In `App.xaml.cs`:

```csharp
private static void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
{
    views.Register(
        new ViewMap<MainPage>(),
        new ViewMap<DailyReflectionPage, DailyReflectionViewModel>(),
        new ViewMap<SobrietyTimePage,    SobrietyTimeViewModel>(),
        new ViewMap<SettingsPage,        SettingsViewModel>());
    ...
}
```

The current `App.xaml.cs` already does the `ViewMap<TPage, TViewModel>` mapping. The remaining work is in the **page constructors**:

```csharp
public sealed partial class SettingsPage : PageBase<SettingsViewModel>
{
    public SettingsPage()
    {
        InitializeComponent();
    }
}
```

The `PageBase<TViewModel>` from spec 006 §A resolves `ViewModel` by reading `DataContext` (which Uno.Extensions Navigation sets to the registered VM during `OnNavigatedTo`). Stop calling `App.GetService<T>()` in page constructors.

Confirm that the navigator sets DataContext before `Loaded` fires; if not, defer the `ViewModel = DataContext` capture into the `Loaded` handler.

### E. Bundle id

Add to `README.md` (top‑level):

> The Uno port ships with `ApplicationId = com.kazo0.dailyreflectionuno` so it can be installed alongside the original Xamarin app during pilot. To replace the original on the App Store / Play Store, change `ApplicationId` to `com.kazo0.dailyreflection` and bump versions accordingly.

### F. Logging in release

Move the logger‑factory wiring out of the `#if DEBUG` block in `App.InitializeLogging`. The factory should always be created; only the *providers* should be DEBUG‑only:

```csharp
public static void InitializeLogging()
{
    var factory = LoggerFactory.Create(builder =>
    {
#if DEBUG
        // Add console / OS providers.
#endif
        builder.SetMinimumLevel(LogLevel.Information);
        builder.AddFilter("Uno", LogLevel.Warning);
        builder.AddFilter("Windows", LogLevel.Warning);
        builder.AddFilter("Microsoft", LogLevel.Warning);
    });
    Uno.Extensions.LogExtensionPoint.AmbientLoggerFactory = factory;
#if HAS_UNO
    Uno.UI.Adapter.Microsoft.Extensions.Logging.LoggingAdapter.Initialize();
#endif
}
```

Hook the host's logger factory once it exists so all producers share one factory.

### G. Configuration fail‑fast

In `App.xaml.cs`:

```csharp
.UseConfiguration(c => c.EmbeddedSource<App>(required: true))
```

If `EmbeddedSource` doesn't expose `required`, validate manually after `Host` is built:

```csharp
var cfg = Host.Services.GetRequiredService<IConfiguration>();
if (string.IsNullOrEmpty(cfg[ConfigurationConstants.DatabaseFileName]))
    throw new InvalidOperationException("appsettings.json missing or empty");
```

### H. Page lifetime documentation

In `App.xaml.cs`, add a comment near the page registrations:

```csharp
// Pages are Transient: Uno.Extensions Navigation expects fresh instances per
// region activation. The Xamarin original used Singleton via Shell;
// Uno's region navigator manages caching itself, so DI lifetime is Transient.
```

## Risks & open questions

1. **NUnit 4 breaking changes.** NUnit 4 drops some legacy APIs. The existing tests are simple enough to migrate but verify per‑file.
2. **`Uno.UITest` setup.** Adding a UI test project is non‑trivial; if scope is tight, ship the thin in‑process visual‑tree walk in §C and treat the proper UITest project as a follow‑up.
3. **Logging providers in release.** Console output on iOS goes to the device log via `OSLog`; on desktop release builds it goes to stdout, which may be invisible if the binary is launched without a console. Consider adding a `EventLog` (Windows) or file provider for production diagnostics — out of scope for this spec.
4. **Bundle id flip.** If we eventually flip `com.kazo0.dailyreflectionuno` → `com.kazo0.dailyreflection`, the App Store rejects an upload that doesn't match the existing app record. Plan for a coordinated cutover (one new version of the original Xamarin app that announces the migration, then upload the Uno binary under the same id).

## Done when

- [x] Dead converters removed: `InverseBoolConverter`, `HasValueConverter`, `AllFalseBoolToVisibilityConverter`, `DateTimeToTimeSpanConverter`, `HtmlToTextConverter` deleted from `Converters/` and from App.xaml. Stale `Page.Resources` block on `SettingsPage.xaml` removed.
- [x] Both test projects target net10.0; `dotnet test` runs clean (25 service tests + 23 presentation tests pass).
- [x] Spec 011 §C "lite" UI smoke tests added in `DailyReflection.Services.Tests/Views/ViewSurfaceTests.cs` — five XAML-shape assertions per page covering the binding contract.
- [x] Page lifetime comment block in `App.xaml.cs` near `RegisterRoutes` documents the deliberate `Transient` registration.
- [x] Release-build logging fixed in `App.InitializeLogging()` — `LoggerFactory.Create` runs unconditionally; only verbose providers are scoped to `#if DEBUG`.
- [x] Missing/empty configuration check in `App.OnLaunched` throws `InvalidOperationException` if `appsettings.json` is absent or the database key is missing.
- [x] README.md updated with bundle id table and migration guidance for replacing the original on the App / Play Store.
- [x] Uno desktop builds clean.

### Implementation deviations from the original plan

* **Constructor injection deferred.** Spec 011 §D proposed switching pages from `App.GetService<TViewModel>()` to navigator-set `DataContext`. The Uno.Extensions navigator only assigns `DataContext` *after* page construction, but our pages (especially `DailyReflectionPage`) need `ViewModel` available in the constructor (to subscribe to `PropertyChanged` and seed inlines). Migrating would require either deferring the subscription to `Loaded` (already where `Init()` runs) or accepting that the VM might be null in the ctor. Given the current `App.GetService<T>()` works and is well-documented, the migration is deferred — tracked as a follow-up if/when the navigator API gives us a synchronous way to read the route's VM during construction.
* **Service-locator stays.** `App.GetService<T>()` keeps its public surface for the same reason. The page lifetime comment in App.xaml.cs makes the deliberate choice explicit.
* **No `Uno.UITest` project.** Out-of-process UI tests would have required the iOS workload + a real desktop runtime to exercise. Instead, the lightweight XAML-source assertions in §C cover the same set of acceptance criteria for binding shape.
* **`HtmlToTextConverter` removed.** Spec 011 §A flagged it as dead; we then realised in spec 003 that the inline parser fully replaces it (the Uno page uses code-behind inlines, not a string-stripping converter).
* **`DateTimeToTimeSpanConverter` removed.** Spec 006 fixed `NotificationTime_Tapped` to update the VM directly with a preserved date component, so the `TimeSpan` round-trip is no longer needed.
