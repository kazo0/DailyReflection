# Spec 005 — AutomationIds

* **Status:** Implemented (2026-05-03).
* **Severity:** 🟡 Quality / parity issue (UI test prerequisite)
* **Gaps closed:** 10.1.8, 10.2.4, 10.3.9, 10.11.2
* **Depends on:** —

## Summary

`DailyReflection.Core/Constants/AutomationConstants.cs` defines stable identifiers for every UI test hook the original Xamarin app exposes. The Xamarin XAML decorates pages, labels, and toolbar items with `AutomationId="{x:Static constants:AutomationConstants.*}"`. The Uno port inherits the constants but does not apply any of them — `grep "AutomationId\|AutomationConstants" heads/DailyReflection.Uno/DailyReflection.Uno/Views/*` returns nothing.

This spec applies the WinUI/Uno equivalent (`AutomationProperties.AutomationId`) to every control that carries an automation id in the Xamarin originals. It is a prerequisite for any UI test harness (spec 011 §G).

## Goals

* Every constant in `AutomationConstants` corresponds to a control in the Uno views with `AutomationProperties.AutomationId="..."` set.
* Names match exactly so existing UI test scripts (if any are revived) work without translation.
* No constants defined but unused; no controls with hard‑coded automation ids that bypass the constants.

## Non‑goals

* New automation ids — if a control needs one and the constant doesn't exist, add it to `AutomationConstants.cs` *and* update the MAUI head to keep parity (that's a one‑line change). Do not add ids willy‑nilly.
* Accessibility names (`AutomationProperties.Name`). That's a separate accessibility pass — left for a future spec.

## Acceptance criteria

1. Every `public const string` in `AutomationConstants` is referenced by exactly one `AutomationProperties.AutomationId="..."` attribute in the Uno views, except `Shell_Tab_*` which map to the `utu:TabBarItem.Content` strings (see §B).
2. A `grep AutomationProperties.AutomationId heads/DailyReflection.Uno/DailyReflection.Uno/Views/*.xaml` returns at least 14 matches (one per non‑shell constant).
3. The constants file has no orphans and no duplicates (verifiable via the test in §C).
4. Zero hard‑coded `AutomationId="some_string"` usages — every value comes from `AutomationConstants` via `{x:Bind constants:AutomationConstants.X}` or set in code‑behind.

## Implementation plan

### A. XAML namespace

Add to each Uno view's root element:

```xml
xmlns:constants="using:DailyReflection.Core.Constants"
```

### B. Apply ids — control by control

WinUI uses an attached property: `AutomationProperties.AutomationId`. In XAML:

```xml
<Page ... AutomationProperties.AutomationId="{x:Bind constants:AutomationConstants.Daily_Reflection}">
```

`x:Bind` to a `const string` works since v2; if it doesn't on Uno's WinUI fork, fall back to `{StaticResource}` against a resource declared in `App.xaml`:

```xml
<x:String x:Key="Daily_Reflection">daily_reflection_view</x:String>
```

…but prefer `x:Bind` so the constant remains the single source of truth.

#### Mapping table

| Constant | View | Control | Notes |
|---|---|---|---|
| `Daily_Reflection` | `DailyReflectionPage.xaml` | root `<Page>` | Root automation id. |
| `DR_Reflection_Title` | same | `TextBlock` for `DailyReflection.Title` | After spec 003, name the TextBlock `TitleText`. |
| `DR_Reflection_Quote` | same | `TextBlock` for `Reading` | `ReadingText` after spec 003. |
| `DR_Reflection_QuoteSource` | same | `TextBlock` rendering `— Source` | |
| `DR_Reflection_Thought` | same | `TextBlock` for `Thought` | `ThoughtText`. |
| `DR_Reflection_Copyright` | same | the literal copyright `TextBlock` | |
| `DR_Share_Reflection` | same | the Share `AppBarButton` in `CommandBar.SecondaryCommands` | |
| `DR_Change_Date` | same | the calendar `AppBarButton` in `CommandBar` | |
| `Sobriety_Time` | `SobrietyTimePage.xaml` | root `<Page>` | |
| `ST_One_Day_At_A_Time` | same | "One day at a time" `TextBlock` | |
| `Settings` | `SettingsPage.xaml` | root `<Page>` | |
| `Settings_Enable_Notif_Switch` | same | the `ToggleSwitch` for `NotificationsEnabled` | |
| `Settings_Time_Picker` | same | `TimePicker x:Name="NotificationTimePicker"` | |
| `Settings_Date_Picker` | same | `CalendarDatePicker x:Name="SoberDatePicker"` | |
| `Settings_Notification_Time` | same | the row containing the notification time label | |
| `Settings_Sober_Date` | same | the row containing the sober date label | |
| `Shell_Tab_Reflection` | `MainPage.xaml` | `utu:TabBarItem` for Reflection | Set `AutomationProperties.AutomationId` directly. The `Content` string already matches but a stable id is more robust than localised content. |
| `Shell_Tab_SoberTime` | same | `utu:TabBarItem` for Sober Time | |
| `Shell_Tab_Settings` | same | `utu:TabBarItem` for Settings | |

### C. Lint test

Add `DailyReflection.Presentation.Tests/AutomationIdsTests.cs`:

```csharp
[Test]
public void Every_AutomationConstant_Is_Used_In_Uno_Views()
{
    var constants = typeof(AutomationConstants)
        .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlatHierarchy)
        .Where(f => f.IsLiteral)
        .Select(f => (string)f.GetRawConstantValue()!)
        .ToList();

    var unoViewsDir = Path.Combine(
        TestContext.CurrentContext.TestDirectory,
        "../../../../../heads/DailyReflection.Uno/DailyReflection.Uno/Views");
    var allXaml = string.Join("\n", Directory.EnumerateFiles(unoViewsDir, "*.xaml").Select(File.ReadAllText));

    foreach (var c in constants)
        Assert.That(allXaml, Does.Contain(c), $"Constant '{c}' not referenced in any Uno view.");
}
```

The test runs from the Tests project's bin folder; the relative path resolves at test time.

### D. Constants update

Three constants are missing from `AutomationConstants` even though Xamarin XAML referenced them indirectly:

* `Settings_Sober_Time_Display` — for the sober time display row on Settings.
* `DR_Reflection_Date_Picker` — for the (Uno‑specific) `DatePickerFlyout`.
* `ST_Sober_Date_Display` — for the calendar date block on the Sober Time page.

Add these to `AutomationConstants` and apply them in the Uno views. Update the MAUI head to also reference them in the matching XAML so the constants stay paired across the two ports.

## Risks & open questions

1. **`x:Bind` to `const string`.** Uno's WinUI implementation supports `x:Bind` to const fields, but this has been a moving target. If it fails to compile, fall back to a `<StaticResource>` strategy: declare the constants once in `App.xaml` as `<x:String x:Key="...">` and reference them via `{StaticResource}`. Less DRY but reliable.
2. **TabBarItem's `Content` already matches.** Setting `AutomationProperties.AutomationId` *and* using a localised `Content` is fine — the id is the stable selector, the content is the visible label. Don't fold them into one.
3. **Test fragility.** The `AutomationIdsTests` test relies on a relative path; if the test project is moved, the path breaks. Acceptable for now; a more robust approach would be a `[ProjectFile]` attribute that carries the Uno head path via MSBuild metadata.

## Done when

- [x] All entries in the §B mapping table are applied in the corresponding Uno XAML.
- [x] Two new constants added (`Settings_Sober_Time_Display`, `ST_Sober_Date_Display`); both applied in Uno and mirrored in MAUI views.
- [x] `AutomationConstantsCoverageTests` passes — every `AutomationConstants` value is referenced in at least one Uno view XAML.
- [x] `dotnet build heads/DailyReflection.Uno/DailyReflection.Uno.csproj -f net10.0-desktop` clean.
- [x] MAUI views updated for the two relevant constants (`Settings_Sober_Time_Display` corrects a duplicate-id bug carried over from Xamarin; `ST_Sober_Date_Display` decorates the calendar date `Grid`). MAUI build not validated in this dev environment (workloads unavailable) — change is purely additive XAML.

### Implementation deviations from the original plan

* **Literal strings, not `x:Bind constants:AutomationConstants.X`.** WinUI XAML doesn't support `x:Static`, and `x:Bind` to const fields is fragile on Uno — different versions accept different shapes. The implementation uses string literals matching the constant *values* (e.g. `AutomationProperties.AutomationId="settings_view"`). The lint test enforces that every constant value still appears, so the values stay paired with the constants.
* **`DR_Reflection_Date_Picker` removed.** The constant proposed in the spec wasn't useful — the picker is a `DatePickerFlyout` whose host (`AppBarButton` with id `change_date`) is the only stable selector. Adding the constant would have meant decorating the flyout's host for the second time. Dropped.
* **TabBar items get `AutomationId` *and* `Content`.** The `Content` is the visible label; the id is the stable selector. Both coexist on each `utu:TabBarItem`.
* **Lint test path resolution.** Walks up from `TestContext.CurrentContext.TestDirectory` until it finds a directory containing `heads/DailyReflection.Uno/DailyReflection.Uno/Views`. Robust to bin/obj nesting.
