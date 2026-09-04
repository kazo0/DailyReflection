# Spec 012 — App theme preference (System / Light / Dark)

* **Status:** Implemented (2026-08-28). Verified on Skia desktop (macOS); Android head compiles; runtime smoke on Android / iOS still to do.
* **Severity:** 🟢 New feature *(not a §10 gap; first requested for the Xamarin app on the never-shipped `origin/app-theme` WIP branch, whose naming this spec reuses)*
* **Gaps closed:** —
* **Depends on:** [004](004-theme-and-shell-chrome.md) (palette), [006](006-settings-correctness.md) (Settings state pattern). [013](013-settings-combobox-rows.md) supplies the picker UI.

## Summary

Add a **Theme** setting — *System*, *Light*, *Dark* — to the Settings tab, persisted like every other setting and applied to the running app immediately, and re-applied at launch. *System* follows the OS theme live.

Three things had to be true for this to work on Uno, and each one is a finding worth keeping:

1. **The theme is applied to the window's root element** (`FrameworkElement.RequestedTheme` on `Window.Content`), the same mechanism as Uno Toolkit's `SystemThemeHelper.SetApplicationTheme(XamlRoot, ElementTheme)`. `Application.RequestedTheme` is never set, so Uno keeps it in sync with the OS.
2. **The root is always pinned to an explicit `Light`/`Dark`.** Resetting it to `ElementTheme.Default` for *System* was tried and rejected: once the root has carried an explicit theme, Uno leaves style‑supplied ThemeResources (the Material `TitleLarge` / `BodyMedium` foregrounds) stale on the *next* OS theme change, while explicit sets re‑theme the whole tree reliably (observed on Uno 6.8 / Skia macOS; a never‑touched root behaves correctly). *System* therefore resolves the current OS theme (`SystemThemeHelper.GetCurrentOsTheme()`) and re‑applies on `UISettings.ColorValuesChanged`, which Uno raises from its OS‑theme hook.
3. **The shell must paint its own background.** No page ever painted one — the bare window colour showed through (`#FFFFFF` light / `#000000` dark), which an in‑app theme can never change, on desktop or on the native mobile windows. `MainPage`'s root `Grid` now paints Material `BackgroundBrush` (`#EFF2F5` / `#121212` — the Xamarin app's own page colours, see `Styles/ColorPaletteOverride.xaml`). Side benefit: on `master` in light mode the white filled cards sat on a white window; they now have contrast.

## Goals

* A `Theme` row in Settings offering System / Light / Dark; default System.
* The choice persists (`PreferenceConstants.AppThemePreference`, stored as the enum's `int`, mirrored to Android SharedPreferences like the other settings) and is honoured at the next launch.
* Light / Dark pin the app regardless of the OS; System follows OS changes while the app is running, in both directions, with every control re‑themed.
* Startup stays side‑effect free in the model (spec 006 / `StartupMigrationRunner` split); the persisted theme is applied once by the app after the window has content.
* Shared layers stay platform‑agnostic: the presentation model only talks to an interface.

## Non‑goals

* Per‑page or per‑control theming; a "high contrast" option.
* Using `Uno.Extensions.Toolkit.IThemeService` (`UseThemeSwitching`). It persists to its own store rather than `ISettingsService`, and it would put a WinUI‑bound service in the Presentation layer. The interface here is deliberately the repo's own.
* Migrating a theme value from the Xamarin app — it never shipped one.

## Acceptance criteria

1. `SettingsModel.AppThemePreference` is an `IState<AppThemePreference>`, initialised from `ISettingsService.Get(PreferenceConstants.AppThemePreference, 0)`; fresh installs (and upgrades from the Xamarin app) read `System`.
2. Setting the state to a value different from the stored one calls `ISettingsService.Set(PreferenceConstants.AppThemePreference, (int)value)` **and** `IAppThemeService.ApplyTheme(value)` exactly once; setting it to the stored value does neither (initial replay is a no‑op, like every other setting).
3. `SettingsModel.AllAppThemePreferences` is `[System, Light, Dark]` in that order.
4. `App.OnLaunched` applies the persisted preference right after `NavigateAsync<MainPage>()` (the root element does not exist before that).
5. With the OS dark and *Light* chosen: the whole app renders light (page background `#EFF2F5`, cards white, all text foregrounds light‑theme) and stays light when the OS is toggled. Symmetric for *Dark* with the OS light.
6. With *System* chosen: toggling the OS light→dark and dark→light re‑themes **every** label, including section headers and card titles (this is the regression that `ElementTheme.Default` exhibits).
7. `MainPage.xaml`'s root `Grid` declares `Background="{ThemeResource BackgroundBrush}"` (lint test).
8. No `#if __ANDROID__` / `#if __IOS__` in Core / Data / Services / Presentation; the only platform code is the head's `AppThemeService`.

## Implementation plan

### A. Data / Core

* `DailyReflection.Data/Models/AppThemePreference.cs` — `enum AppThemePreference { System = 0, Light = 1, Dark = 2 }`. The numeric values are the stored contract: append, never renumber.
* `DailyReflection.Core/Constants/PreferenceConstants.cs` — `AppThemePreference = "AppThemePreference"` (the key the `origin/app-theme` WIP used).
* `DailyReflection.Core/Constants/AutomationConstants.cs` — `Settings_Theme_Pref = "settings_theme_pref"` (must appear in a view; the coverage lint test enforces it).

### B. Service contract

`DailyReflection.Services/Theme/IAppThemeService.cs`:

```csharp
public interface IAppThemeService
{
	void ApplyTheme(AppThemePreference preference); // safe from any thread; no-op before the window has content
}
```

Named `IAppThemeService` rather than `IThemeService` because the head has a global using for `Uno.Extensions.Toolkit`, whose `IThemeService` would make the short name ambiguous.

### C. Presentation

`SettingsModel` gains the state, its `_lastThemePreference` mirror, `AllAppThemePreferences`, and the side effect:

```csharp
private async ValueTask OnAppThemePreferenceChanged(AppThemePreference value, CancellationToken ct)
{
	if (value == _lastThemePreference) return;
	_lastThemePreference = value;
	_settingsService.Set(PreferenceConstants.AppThemePreference, (int)value);
	_themeService.ApplyTheme(value);
	await Task.CompletedTask;
}
```

The constructor takes `IAppThemeService` as its last parameter; DI resolves it from the head.

### D. Head — `PlatformServices/AppThemeService.cs`

* Registered singleton in `DependencyInjection/Dependencies.cs` (`AddPlatformServices`).
* Holds a `UISettings` instance (field, so the subscription stays alive) and the current preference.
* `ApplyTheme` stores the preference and dispatches `Apply` to the UI thread (`Window.DispatcherQueue`; MVUX `ForEach` side effects run off it).
* `Apply` sets `root.RequestedTheme = ToElementTheme(preference)` on `MainWindow.Content`; `ToElementTheme` maps Light/Dark directly and resolves *System* through `SystemThemeHelper.GetCurrentOsTheme()`.
* `UISettings.ColorValuesChanged` re‑runs `Apply` when the preference is *System*.

### E. Head — startup

`App.OnLaunched`, immediately after `Host = await builder.NavigateAsync<MainPage>()`:

```csharp
ApplyPersistedTheme(Host.Services); // ISettingsService.Get(...) -> IAppThemeService.ApplyTheme
```

Placed here, not in the model, because the model's initial replay is deliberately side‑effect free and because the root element only exists after navigation. (Uno's own `ThemeService` applies on window activation — same timing.)

### F. Head — shell background

`Views/MainPage.xaml`: root `Grid` gets `Background="{ThemeResource BackgroundBrush}"` with a comment explaining why (bare window colour never follows an in‑app theme).

### G. Tests

* `DailyReflection.Presentation.Tests/Models/SettingsModelTests.cs` — defaults to System; retrieved on load; set persists + applies; setting the stored value is a no‑op; `AllAppThemePreferences` order; load applies nothing. `SobrietyTimeModelTests` updated for the new constructor parameter.
* `DailyReflection.Services.Tests/Views/ViewSurfaceTests.cs` — `MainPage_paints_a_theme_aware_background`; the Settings picker assertions live in spec 013.
* `AutomationConstantsCoverageTests` picks up `Settings_Theme_Pref` automatically.

## Risks & open questions

1. **First‑frame flash on cold launch for pinned themes.** The theme is applied after navigation, so the first frame(s) can render in the OS theme. The native splash covers this on mobile; on desktop it is at most a couple of frames. Setting `Application.RequestedTheme` (or `Uno.UI.ApplicationHelper.RequestedCustomTheme`) in the `App` constructor would remove it but would also lock the application theme, which breaks *System* later in the session. Accepted.
2. **`ColorValuesChanged` ordering.** The implementation reads the OS theme through `SystemThemeHelper.GetCurrentOsTheme()` rather than `Application.Current.RequestedTheme`, so it does not depend on Uno updating the latter before raising the event.
3. **Mobile runtime.** Android compiles; neither Android nor iOS has had a runtime smoke test of the switch or of the OS‑change path. CI's mobile jobs cover the build only.
4. **Uno upgrade.** If a future Uno fixes the `ElementTheme.Default` staleness, the explicit‑pin approach still works; don't switch back without re‑running criterion 6.

## Done when

- [x] `AppThemePreference` enum, `PreferenceConstants.AppThemePreference`, `AutomationConstants.Settings_Theme_Pref`.
- [x] `IAppThemeService` in Services; `AppThemeService` in the head, registered in `AddPlatformServices`.
- [x] `SettingsModel.AppThemePreference` state + `AllAppThemePreferences` + side effect.
- [x] `App.OnLaunched` applies the persisted theme after navigation.
- [x] `MainPage` root paints `BackgroundBrush`.
- [x] Theme row in Settings (see spec 013 for the control).
- [x] Unit tests: 6 new presentation tests, 1 new view‑surface test; suites green.
- [x] Desktop: Light / Dark pin, System follows OS both ways with all labels re‑themed, persisted theme honoured on restart — verified by screenshot on Skia macOS.
- [x] Android head compiles.
- [ ] Android / iOS runtime smoke test (manual).

### Implementation deviations from the original plan

* **`ElementTheme.Default` abandoned** for *System* after it left `TitleLarge` / `BodyMedium` foregrounds stale on an OS change (see Summary §2). The service now pins an explicit theme and listens to `UISettings.ColorValuesChanged`.
* **Root background added** after the first Light run showed white text on a black window: the pages had never painted a background, which only became visible once the theme could diverge from the OS.
* **Picker UI replaced.** The first cut copied the existing "tap a card, reveal a hidden `ComboBox`" pattern; the owner rejected hidden controls, which became spec 013.
* **Incidental fixes on the way:** `DailyReflectionPage.xaml` had an unterminated `YearVisible="False` attribute on `master` (CI red after PR #7) — fixed; AGENTS.md corrected on line endings (`text=auto` → LF on macOS checkouts), the multi‑platform `-p:TargetFrameworkOverride=android%3Bdesktop` syntax, the `Styles/` listing and test counts.
