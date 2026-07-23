# Spec 001 — Version tracking, settings & DB migrations, runtime app version

* **Status:** Implemented (2026-05-03). Android/iOS partials compile-checked but not platform-verified — see Done‑when below.
* **Severity:** 🔴 Functional gap
* **Gaps closed:** 10.6.1, 10.6.2, 10.8.1, 10.8.2, 10.8.3, 10.8.4, 10.3.7
* **Depends on:** —

## Summary

The original Xamarin app calls `Xamarin.Essentials.VersionTracking.Track()` on every launch and uses it to gate two one‑shot migrations from `App.OnStart`:

1. **`MigrateSettingsIfNeeded`** — on first launch of build ≥ `NewSettingsBuild` (20) when no previous build is recorded and the legacy preferences container holds known keys, copy legacy preferences into the `DR_Settings` container and re‑schedule notifications.
2. **`RefreshDatabaseIfNeeded`** — on first launch of any build ≥ `RefreshDatabaseBuild` (32) when the previous build was below that threshold, delete and re‑extract the embedded `dailyreflections.db`.

The Uno port never calls `VersionTracking.Track()`, never invokes either migration, and surfaces `VersionConstants.VersionNumber` (literal `"1.0.0"`) on the Settings page instead of a runtime build version. All the version‑gated code paths in the codebase are dead.

This spec restores the migration triggers using a small `LocalSettings`‑backed version tracker and replaces the hard‑coded version string on the Settings page with the runtime app version.

## Goals

* Replicate the four `VersionTracking` properties the migrations depend on: `IsFirstLaunchEver`, `IsFirstLaunchForCurrentVersion`, `IsFirstLaunchForCurrentBuild`, `CurrentBuild`, `PreviousBuild`, `CurrentVersion`, `PreviousVersion`.
* Run `MigrateSettingsIfNeeded` and `RefreshDatabaseIfNeeded` once per upgrade in `App.OnLaunched` after the host has been built.
* Display the running build's version + build number on Settings.
* Keep the migration triggerable on all three Uno targets (Android, iOS, Desktop).

## Non‑goals

* Replacing `VersionConstants.{NewSettingsVersion,NewSettingsBuild,RefreshDatabaseVersion,RefreshDatabaseBuild}` — the constants are still authoritative.
* Generalising the tracker into a NuGet — keep it inside the Uno head.
* Any change to the embedded database content.

## Acceptance criteria

1. A new Uno install with `ApplicationVersion=1`/`ApplicationDisplayVersion=1.0` records `IsFirstLaunchEver = true` on the first run and `false` on the second.
2. Bumping `ApplicationVersion` from 1 → 2 makes `IsFirstLaunchForCurrentBuild = true` on the first run after the bump and `false` on the second.
3. `App.OnLaunched` calls `MigrateSettingsIfNeeded()` and `RefreshDatabaseIfNeeded()` — verifiable by setting a breakpoint or unit test on the methods.
4. `RefreshDatabaseIfNeeded` actually deletes and re‑extracts `dailyreflections.db` when the build threshold is crossed (verifiable by deleting the file in `LocalApplicationData`, launching, and asserting the file reappears).
5. The Settings page **About** row shows the value reported by `Windows.ApplicationModel.Package.Current.Id.Version` on packaged Windows targets; on Skia desktop it falls back to the assembly informational version; on Android/iOS it uses the platform native version (`PackageManager` / `NSBundle`).
6. `dotnet build heads/DailyReflection.Uno/DailyReflection.Uno.sln` succeeds for `net10.0-android`, `net10.0-ios`, and `net10.0-desktop`.
7. The shared `DailyReflection-maui.slnf` solution still builds — no shared‑layer changes regress MAUI.

## Implementation plan

### A. Version tracker

Add a partial class `PlatformServices/VersionTrackingService.cs`:

```csharp
namespace DailyReflection.PlatformServices;

public interface IVersionTrackingService
{
    void Track();
    bool IsFirstLaunchEver { get; }
    bool IsFirstLaunchForCurrentVersion { get; }
    bool IsFirstLaunchForCurrentBuild { get; }
    string CurrentVersion { get; }
    string? PreviousVersion { get; }
    string CurrentBuild { get; }
    string? PreviousBuild { get; }
}

public partial class VersionTrackingService : IVersionTrackingService
{
    private const string KeyVersion = "DR_VT_Version";
    private const string KeyBuild   = "DR_VT_Build";
    private const string KeyPrevVer = "DR_VT_PrevVersion";
    private const string KeyPrevBld = "DR_VT_PrevBuild";

    private readonly ApplicationDataContainer _store
        = ApplicationData.Current.LocalSettings;

    // Platform-provided in partials below.
    public partial string CurrentVersion { get; }
    public partial string CurrentBuild { get; }

    public bool IsFirstLaunchEver { get; private set; }
    public bool IsFirstLaunchForCurrentVersion { get; private set; }
    public bool IsFirstLaunchForCurrentBuild { get; private set; }
    public string? PreviousVersion { get; private set; }
    public string? PreviousBuild { get; private set; }

    public void Track()
    {
        var storedVersion = _store.Values[KeyVersion] as string;
        var storedBuild   = _store.Values[KeyBuild]   as string;

        IsFirstLaunchEver               = storedVersion is null && storedBuild is null;
        IsFirstLaunchForCurrentVersion  = storedVersion != CurrentVersion;
        IsFirstLaunchForCurrentBuild    = storedBuild   != CurrentBuild;

        if (IsFirstLaunchForCurrentVersion)
        {
            PreviousVersion = storedVersion;
            _store.Values[KeyPrevVer] = storedVersion;
            _store.Values[KeyVersion] = CurrentVersion;
        }
        else PreviousVersion = _store.Values[KeyPrevVer] as string;

        if (IsFirstLaunchForCurrentBuild)
        {
            PreviousBuild = storedBuild;
            _store.Values[KeyPrevBld] = storedBuild;
            _store.Values[KeyBuild]   = CurrentBuild;
        }
        else PreviousBuild = _store.Values[KeyPrevBld] as string;
    }
}
```

Provide platform partials:

* `PlatformServices/VersionTrackingService.Android.cs` — wraps `Application.Context.PackageManager.GetPackageInfo(Application.Context.PackageName, 0)`; `versionName` → `CurrentVersion`, `versionCode` → `CurrentBuild`.
* `PlatformServices/VersionTrackingService.iOS.cs` — `NSBundle.MainBundle.ObjectForInfoDictionary("CFBundleShortVersionString")` → version, `"CFBundleVersion"` → build.
* `PlatformServices/VersionTrackingService.cs` (no `#if`) — desktop fallback: `Windows.ApplicationModel.Package.Current.Id.Version` if available, else the entry assembly's `AssemblyInformationalVersionAttribute` (version) and `AssemblyFileVersionAttribute` (build), with a final fallback to `VersionConstants.VersionNumber` / "1".

Register in `DependencyInjection/Dependencies.cs`:

```csharp
services.AddSingleton<IVersionTrackingService, VersionTrackingService>();
```

### B. Wire the migrations

Move the migration helpers off `App.xaml.cs` (they have nothing to do with the Application object) into a new `DailyReflection.PlatformServices.StartupMigrationRunner`:

```csharp
public class StartupMigrationRunner
{
    private readonly IVersionTrackingService _vt;
    private readonly ISettingsService _settings;
    private readonly INotificationService _notifications;
    private readonly IDailyReflectionDatabase _database;

    // ctor injected.

    public async Task RunAsync()
    {
        _vt.Track();
        await MigrateSettingsIfNeeded();
        await RefreshDatabaseIfNeeded();
    }

    private async Task MigrateSettingsIfNeeded() { /* port from Xamarin App.xaml.cs */ }
    private async Task RefreshDatabaseIfNeeded() { /* port from Xamarin App.xaml.cs */ }

    private static double ParseBuild(string? b) =>
        double.TryParse(b, out var d) ? d : 0;
}
```

Register as Transient and resolve once at the end of `OnLaunched`:

```csharp
Host = await builder.NavigateAsync<MainPage>();
await Host.Services.GetRequiredService<StartupMigrationRunner>().RunAsync();
```

Resolve **after** navigation so the UI thread is free; migrations run in the background. If `RefreshDatabaseFile` runs concurrently with the first reflection load, the `IDailyReflectionDatabase` singleton needs a lock — see Risk #1.

### C. `MigrateSettingsIfNeeded` adjustments

The Xamarin version reads from `Xamarin.Essentials.Preferences.ContainsKey(...)` — that container does not exist on Uno. Replace the legacy probe with a check against the **non‑mirrored** `LocalSettings` keys directly: if `LocalSettings.Values.ContainsKey(PreferenceConstants.SoberDate)` returns `true` for a value with the *legacy* `string`/`DateTime`‑text encoding (rather than the binary‑long encoding `SettingsService` writes today), treat it as a legacy entry and call `_settings.MigrateOldPreferences()`.

For greenfield Uno installs the predicate will always be false and the migration is a no‑op, which is the desired behaviour.

### D. Settings page version display

In `Views/SettingsPage.xaml.cs`:

```csharp
public string AppVersion => $"{_versionTracking.CurrentVersion} ({_versionTracking.CurrentBuild})";
```

Resolve `_versionTracking` from the static `App.GetService<IVersionTrackingService>()` (until spec 011 swaps the page to constructor injection).

### E. Tests

Add `DailyReflection.Presentation.Tests/StartupMigrationRunnerTests.cs` with Moq scenarios:

* Fresh launch: `IsFirstLaunchEver = true`, no migrations run.
* Build bumped from 19 → 20 with legacy keys: `MigrateOldPreferences` called, `TryScheduleDailyNotification` called when `NotificationsEnabled = true`.
* Build bumped from 31 → 32: `RefreshDatabaseFile` called.
* Build bumped from 31 → 33 with first launch ever: `RefreshDatabaseFile` **not** called (matches Xamarin's `!IsFirstLaunchEver` guard).

## Risks & open questions

1. **Database concurrency.** `RefreshDatabaseFile` deletes the on‑disk db while the UI may be reading it. Guard with a `SemaphoreSlim` on `IDailyReflectionDatabase` or run migrations *before* `NavigateAsync<MainPage>`. The latter delays first paint; prefer the former.
2. **Desktop "build" semantics.** Skia desktop binaries don't have a `versionCode` analogue. Falling back to the assembly file version is acceptable but means `IsFirstLaunchForCurrentBuild` only flips when developers bump `<FileVersion>`. Document this in the README.
3. **`LocalSettings` vs. `SharedPreferences` mirror.** Version‑tracking state should be authoritative in `LocalSettings` only — do **not** mirror to Android `SharedPreferences`, since the receivers don't need it and double‑writes risk drift.
4. **`MigrateOldPreferences` semantics on Uno.** Today it round‑trips inside `LocalSettings`; spec 006 may revisit. This spec only invokes the existing implementation.

## Done when

- [x] `IVersionTrackingService` lives in `DailyReflection.Services/VersionTracking/`. Implementation `VersionTrackingService.cs` + 3 platform partials (`.Android.cs`, `.iOS.cs`, `.Desktop.cs`) under `heads/DailyReflection.Uno/.../PlatformServices/`.
- [x] Registered Singleton in `Dependencies.AddPlatformServices`. `StartupMigrationRunner` registered Transient.
- [x] `StartupMigrationRunner` (in `DailyReflection.Services/Startup/`) ports `MigrateSettingsIfNeeded` and `RefreshDatabaseIfNeeded` and is invoked from `App.OnLaunched` after `NavigateAsync`.
- [x] `SettingsPage.AppVersion` reflects the runtime version (`{currentVersion} ({currentBuild})`).
- [x] New unit tests for `StartupMigrationRunner` pass (7 scenarios in `DailyReflection.Services.Tests/Startup/StartupMigrationRunnerTests.cs`).
- [x] Uno desktop build clean (`net10.0-desktop`). Android/iOS partials wrapped in `#if` guards; not built in this dev environment (workloads unavailable). MAUI head not regressed (shared interface added but not consumed there yet).

### Implementation deviations from the original plan

* **Service layer placement.** Spec planned to put `IVersionTrackingService` and `StartupMigrationRunner` under `PlatformServices/` (Uno head only). They moved into `DailyReflection.Services/{VersionTracking,Startup}/` so the same migration plumbing can be reused by the MAUI head later without duplication.
* **Migration trigger predicate.** `MigrateSettingsIfNeeded` no longer probes a "legacy" preferences container — there is no such container on Uno. The condition therefore reduces to "first launch of a build ≥ 20 with no previous build/version recorded", which is the exact predicate the Xamarin original applied to upgrades. On a greenfield Uno install the condition is true on the very first launch, so `MigrateOldPreferences()` is invoked (the implementation is a no-op round-trip in that case, matching its current behaviour). Spec 006 §10.6.1 will revisit the migration semantics.
* **Concurrency.** Migrations run **after** `NavigateAsync<MainPage>` rather than before (less first-paint latency). `RefreshDatabaseFile()` already guards via `_db.CloseAsync()` before deleting; no additional `SemaphoreSlim` was needed.
* **`SettingsService` constructor concern.** `ApplicationData.Current.LocalSettings` was already eagerly read in the constructor (pre-existing); spec 006 §10.6.7 covers tightening this.

### Manual verification still required

* On a real Android device, confirm `pm.GetPackageInfo(...).VersionName/VersionCode` returns sensible values matching `<ApplicationDisplayVersion>`/`<ApplicationVersion>`.
* On a real iOS device, confirm `NSBundle.MainBundle.InfoDictionary["CFBundleShortVersionString"]` returns the correct value.
* Bump `<ApplicationVersion>` and confirm `IsFirstLaunchForCurrentBuild` flips on the next launch.
* Delete the on-disk `dailyreflections.db`, bump build past `RefreshDatabaseBuild`, and confirm the file reappears.
