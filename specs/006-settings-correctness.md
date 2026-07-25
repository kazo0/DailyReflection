# Spec 006 — Settings page correctness

* **Status:** Implemented (2026-05-03).
* **Severity:** 🔴 / 🟠
* **Gaps closed:** 10.2.1, 10.2.2, 10.2.5, 10.2.6, 10.3.1, 10.3.4, 10.3.5, 10.3.6, 10.3.8, 10.1.10
* **Depends on:** [005](005-automation-ids.md) *(recommended; otherwise the lifecycle changes here will need a second pass to add automation ids)*

## Summary

The Settings page on the Uno port has five distinct correctness issues that share a single root: lifecycle hygiene was lost in the port from `OnAppearing`/`OnDisappearing` (Xamarin) to `Loaded`/`Unloaded` (WinUI/Uno) — and the conversion was incomplete. While we're in the area, two minor regressions on the Sober Time page are folded in (they share the same VM activation pattern), plus a stray copy‑paste resource on the Daily Reflection page.

| # | Issue | Severity |
|---|---|---|
| 10.3.1 | `WeakReferenceMessenger` recipients leak: registered in ctor, never unregistered. | 🔴 |
| 10.3.4 | `NotificationTime` setter rebases the persisted DateTime to today on every change. | 🟠 |
| 10.3.5 | Notification time row is greyed out on Android when notifications are off; original was always tappable on Android. | 🟠 |
| 10.3.6 | `MaxDate` uses `DateTimeOffset` while VM exposes `DateTimeOffset` — fragile against future type changes; also the local/utc boundary is undefined. | 🟠 |
| 10.3.8 | Three event handlers in code‑behind for the three settings rows. (Refactor only.) | 🟡 |
| 10.2.1 | Sober Time `SoberDate` null→visible transition relies on `[ObservableProperty]` re‑firing after a settings change. | 🟠 |
| 10.2.2 | `SobrietyTimePage` never deactivates the VM. | 🟠 |
| 10.2.5 | `IsDisplayPreferenceToBoolConverter` parameterised by string instead of typed enum. | 🟡 |
| 10.2.6 | Empty `OnUnloaded` handler. | 🟡 |
| 10.1.10 | Stray `<converters:PluralityConverter x:Key="YearsPluralityConverter">` in `DailyReflectionPage.xaml` resources. | 🟡 |

## Goals

* Page navigation never accumulates messenger recipients.
* `NotificationTime` persists with a stable date component.
* Android UX matches the original: time row remains tappable even when notifications are off.
* Sober Time page activates / deactivates the VM symmetrically.
* All converter parameters are typed.

## Non‑goals

* Replacing `TableView` with native settings chrome (covered separately if at all — see §10.3.3, deferred).
* Restructuring the static `App.GetService<T>()` resolution (spec 011 §B).

## Acceptance criteria

1. Navigating away from `SettingsPage` and back N times leaves exactly **one** `NotificationPermissionRequestMessage` recipient registered. Verifiable via reflection on `WeakReferenceMessenger.Default`'s recipient table or via a unit test using a custom messenger.
2. `SettingsViewModel.NotificationTime = new DateTime(2026, 5, 3, 8, 0, 0)` followed by a tap that changes the time to 09:00 leaves the persisted value as `2026-05-03 09:00:00`, not `Today 09:00:00`.
3. On Android, with `NotificationsEnabled = false`, tapping the **Time** row opens the time picker (matching the Xamarin `TimePickerLabelEnabledConverter` behaviour). On iOS / desktop, the row stays disabled when notifications are off (matching the original).
4. `IsDisplayPreferenceToBoolConverter` accepts a `SoberTimeDisplayPreference` enum value as `Parameter`, not a string.
5. After changing the sober date in Settings and switching to the Sober Time tab, the calendar block becomes visible without a page rebuild.
6. `SobrietyTimePage` sets `vm.IsActive = false` on `Unloaded` and back to `true` on `Loaded` (symmetric).
7. The stray `YearsPluralityConverter` resource is removed from `DailyReflectionPage.xaml`.

## Implementation plan

### A. Lifecycle base class

Both `SettingsPage` and `SobrietyTimePage` (and arguably `DailyReflectionPage`) need the same pattern:

* On `Loaded`: register messenger handlers, set `vm.IsActive = true`.
* On `Unloaded`: unregister, set `vm.IsActive = false`.

Add `Views/PageBase.cs`:

```csharp
namespace DailyReflection.Views;

public abstract class PageBase<TViewModel> : Page where TViewModel : ViewModelBase
{
    public TViewModel ViewModel { get; }

    protected PageBase()
    {
        ViewModel = App.GetService<TViewModel>();
        DataContext = ViewModel;
        Loaded   += (_, _) => OnPageLoaded();
        Unloaded += (_, _) => OnPageUnloaded();
    }

    protected virtual void OnPageLoaded()
    {
        ViewModel.IsActive = true;
        RegisterMessages();
    }

    protected virtual void OnPageUnloaded()
    {
        UnregisterMessages();
        ViewModel.IsActive = false;
    }

    protected virtual void RegisterMessages() { }
    protected virtual void UnregisterMessages() =>
        WeakReferenceMessenger.Default.UnregisterAll(this);
}
```

Refactor each page to derive from `PageBase<TViewModel>` and override `RegisterMessages` for `SettingsPage`. The constructor stops doing direct DI lookups.

### B. `NotificationTime` setter (10.3.4)

Fix in `Views/SettingsPage.xaml.cs` (no shared‑layer change):

```csharp
private void NotificationTimePicker_TimeChanged(object sender, TimePickerValueChangedEventArgs e)
{
    var current = ViewModel.NotificationTime;
    ViewModel.NotificationTime = new DateTime(
        current.Year, current.Month, current.Day,
        e.NewTime.Hours, e.NewTime.Minutes, 0, current.Kind);
    NotificationTimePicker.Visibility = Visibility.Collapsed;
}
```

This preserves the date component even though only `TimeOfDay` is functionally used. It keeps the persisted DateTime stable across "I changed the time at midnight" edge cases.

### C. Android tappable rule (10.3.5)

Restore `TimePickerLabelEnabledConverter` in `DailyReflection.Uno/Converters/`:

```csharp
public sealed class TimePickerLabelEnabledConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, string language)
    {
        if (OperatingSystem.IsAndroid()) return true;
        return value is bool b && b;
    }
    public object? ConvertBack(object? v, Type t, object? p, string l) => throw new NotImplementedException();
}
```

In `Views/SettingsPage.xaml`, change the `IsEnabled` binding on the notification time `Grid`:

```xml
<Grid ... IsEnabled="{x:Bind ViewModel.NotificationsEnabled, Mode=OneWay,
                      Converter={StaticResource TimePickerLabelEnabledConverter}}">
```

…and update `NotificationTime_Tapped` to early‑return only on platforms where the gate matters:

```csharp
if (!OperatingSystem.IsAndroid() && !ViewModel.NotificationsEnabled) return;
```

### D. `IsDisplayPreferenceToBoolConverter` typed parameter (10.2.5)

Update `Converters/IsDisplayPreferenceToBoolConverter.cs` so its `DisplayPreference` property is typed:

```csharp
public sealed class IsDisplayPreferenceToBoolConverter : IValueConverter
{
    public SoberTimeDisplayPreference DisplayPreference { get; set; }

    public object Convert(object? value, Type targetType, object? parameter, string language)
        => value is SoberTimeDisplayPreference v && v == DisplayPreference
            ? Visibility.Visible : Visibility.Collapsed;

    public object? ConvertBack(object? v, Type t, object? p, string l) => throw new NotImplementedException();
}
```

In `Views/SobrietyTimePage.xaml` replace:

```xml
DisplayPreferenceString="DaysMonthsYears"
```

with:

```xml
DisplayPreference="{x:Bind models:SoberTimeDisplayPreference.DaysMonthsYears}"
```

`x:Bind` to a static enum value works in WinUI 3; if Uno doesn't support that yet (verify), fall back to declaring the converter instances in code‑behind and assigning the typed enum there.

### E. Sober Time page activation (10.2.2, 10.2.6)

`PageBase<TViewModel>` from §A handles this. Delete the empty `OnUnloaded` handler from `SobrietyTimePage.xaml.cs`.

### F. SoberDate visibility (10.2.1)

After the lifecycle changes in §A, `IsActive` is correctly toggled, so the messenger‑driven update from `SettingsViewModel` reaches `SobrietyTimeViewModel`'s recipient interface. Add an integration test:

```csharp
[Test]
public async Task Setting_SoberDate_From_Settings_Updates_SobrietyTime_Display()
{
    var settings = container.Resolve<SettingsViewModel>();
    var sober   = container.Resolve<SobrietyTimeViewModel>();
    sober.IsActive = true;

    settings.SoberDate = DateTime.Today.AddDays(-30);

    Assert.That(sober.SoberDate, Is.EqualTo(DateTime.Today.AddDays(-30)));
    Assert.That(sober.TotalDaysSober, Is.EqualTo(30));
}
```

### G. Stray resource cleanup (10.1.10)

Delete the `<converters:PluralityConverter x:Key="YearsPluralityConverter">` from `Views/DailyReflectionPage.xaml`'s `Page.Resources` — it is unused on that page.

### H. Settings code‑behind cleanup (10.3.8 — refactor only)

The three `*_Tapped` handlers are kept (they're the simplest way to toggle visibility on a hidden picker). No change. The 🟡 row is informational only.

## Risks & open questions

1. **`PageBase<TViewModel>` and XAML.** Generic page base classes complicate XAML compilation. The fix is to use a non‑generic `PageBase` that exposes a `ViewModel` property as `object` and have the derived page cast in code‑behind. Verify which approach Uno's XAML compiler prefers; the current scaffolding might require the generic to be unwrapped via a non‑generic intermediate class (`SettingsPage : PageBase`, with `SettingsPage` exposing a typed `ViewModel`).
2. **`x:Bind` to enum static.** If unsupported, fall back to converter‑side string parsing but make the property `SoberTimeDisplayPreference` so the parser is internal:

   ```csharp
   public string DisplayPreferenceName { get; set; } = "";
   private SoberTimeDisplayPreference Preference =>
       Enum.Parse<SoberTimeDisplayPreference>(DisplayPreferenceName);
   ```

3. **Messenger lifetime test.** Inspecting `WeakReferenceMessenger`'s internal recipient table requires reflection. Better: use a fresh `IMessenger` instance per test and inject it.

## Done when

- [x] Non-generic `PageBase` (XAML root) lands; `SettingsPage`, `SobrietyTimePage`, `DailyReflectionPage` all derive from it via `<views:PageBase x:Class="...">` so the XAML-generated partial inherits the right base.
- [x] `WeakReferenceMessenger` registrations move into `RegisterMessages`; `UnregisterMessages` (default = `UnregisterAll(this)`) runs on `Unloaded`.
- [x] `NotificationTime` setter preserves the persisted date component.
- [x] `TimePickerLabelEnabledConverter` re-introduced in `Converters/`, registered in `App.xaml`, applied to the Notification Time row's `IsEnabled`.
- [x] `IsDisplayPreferenceToBoolConverter.DisplayPreference` consumed as a typed enum directly in XAML (`DisplayPreference="DaysMonthsYears"`); the string-indirection is no longer used.
- [x] Empty `OnUnloaded` removed from `SobrietyTimePage.xaml.cs`.
- [x] Stray `YearsPluralityConverter` removed from `DailyReflectionPage.xaml` (already done in spec 003).
- [x] Integration test `MessagePropagationTests.Setting_SoberDate_Updates_Active_SobrietyTime_VM` (and the inactive-VM counterpart) pass.
- [x] Uno desktop build clean.

### Implementation deviations from the original plan

* **Non-generic `PageBase`.** Per the spec's risk note, generic page bases conflict with the XAML-generated partial that hardcodes `: Page`. The implementation uses a non-generic `PageBase : Page` with an abstract `ActiveViewModel` for activation. Each concrete page exposes its own typed `ViewModel` property. The XAML root element switches from `<Page>` to `<views:PageBase>` so the generator emits `: PageBase` in the partial.
* **VM activation moved to `Loaded`.** Constructors no longer set `IsActive = true` directly; the lifecycle is now symmetric.
* **Android tappable rule.** Restored via `TimePickerLabelEnabledConverter`. Code-behind `NotificationTime_Tapped` mirrors the rule (always tap-through on Android; gated elsewhere).
