# Spec 002 — Desktop notifications

* **Status:** Implemented (2026-05-03) with deliberate scope reduction — see "Implementation deviations".
* **Severity:** 🔴 Functional gap
* **Gaps closed:** 10.5.1, 10.3.2
* **Depends on:** —

## Summary

`PlatformServices/NotificationService.cs` (the un‑guarded file used by `net10.0-desktop`) is a hard no‑op: it returns `false` from `CanScheduleNotifications`/`TryScheduleDailyNotification` and is empty for `CancelNotifications`/`ShowNotificationSettings`. The Settings page nevertheless allows the user to toggle `NotificationsEnabled = true` and pick a time. The user gets no notifications, no error, and no indication that the feature is unsupported. The "OK" path of the permission dialog also opens nothing because `ShowNotificationSettings` is empty.

This spec implements a real desktop notification path on Windows (toast via `Microsoft.Windows.AppNotifications`) and gracefully degrades on macOS / Linux Skia targets. On platforms where scheduling is genuinely impossible, the UI surfaces that fact — the toggle becomes disabled and a one‑line caption explains why.

## Goals

* Daily local notifications fire on Windows desktop when the user enables them.
* `ShowNotificationSettings()` opens **Settings > Notifications & actions** on Windows.
* On macOS desktop and Linux X11/framebuffer, the notification toggle is disabled with an explanatory caption; no false positives.
* `IsSupported` becomes a real signal the UI can bind to.

## Non‑goals

* macOS native bundle notifications via `UNUserNotificationCenter` — the Skia macOS host is not packaged and lacks the entitlements required to authenticate as a notification provider. Listed as a follow‑up in §Open questions.
* Linux toast support via `libnotify` — same reasoning; deferred.
* Cross‑process scheduling on desktop (i.e. firing notifications when the app is closed) — the desktop targets do not have a daemon equivalent of Android's `AlarmManager`. The first iteration only fires while the app is running; the spec calls this out so users aren't surprised.

## Acceptance criteria

1. On Windows (`net10.0-desktop` running on Win32), enabling notifications and choosing a time `T` causes a toast titled "Daily Reflection" with body "Time for the daily reflection!" to fire at the next occurrence of `T` (verified via test build with the time set 1 minute in the future). The toast is dismissable; tapping it brings the app to the foreground.
2. After firing once, the toast re‑schedules for the next day (covers the rolling‑window behaviour the Android receiver provides). If the app is closed before the next firing, the toast does **not** fire (documented limitation).
3. `ShowNotificationSettings()` on Windows launches `ms-settings:notifications`.
4. On macOS desktop / Linux desktop, `CanScheduleNotifications()` returns `false`, `TryScheduleDailyNotification()` returns `false` immediately without throwing, and the `SettingsViewModel.NotificationsEnabled` setter cannot be turned on (the toggle is disabled in XAML — see §Implementation B).
5. `INotificationService.IsSupported` (new) returns the boolean the UI binds to.
6. The permission‑dialog flow on desktop correctly reports failure: the dialog is never shown if scheduling is unsupported (the VM short‑circuits), and *is* shown on Windows when authorisation is required.

## Implementation plan

### A. Interface change

Add a property to `INotificationService` (in shared `DailyReflection.Services/Notification/INotificationService.cs`):

```csharp
bool IsSupported { get; }
```

Update every implementation:

* iOS / Android: `IsSupported => true` (existing behaviour).
* Desktop fallback: returns `false` on macOS / Linux, `true` on Windows once spec 002 lands.

The shared `Services` library only adds the property to the interface — no behaviour change there. Verify the MAUI head's `NotificationService.{Android,iOS}.cs` files compile after the interface gains a new member; add `IsSupported => true` there too.

### B. Bind from the Settings page

In `Views/SettingsPage.xaml`:

```xml
<ToggleSwitch IsOn="{x:Bind ViewModel.NotificationsEnabled, Mode=TwoWay}"
              IsEnabled="{x:Bind ViewModel.NotificationsSupported, Mode=OneWay}" />
```

Plus a `<TextBlock>` caption visible only when `NotificationsSupported = false`:

> "Notifications aren't supported on this platform. The reflection will still load when the app is open."

Add `NotificationsSupported` (read‑only `bool`) to `SettingsViewModel` in the shared `Presentation` project. Compute from `_notificationService.IsSupported`.

### C. Windows implementation

Split the desktop fallback file:

* `PlatformServices/NotificationService.cs` — keep, but make it the **macOS/Linux** no‑op explicitly: `#if !(__ANDROID__ || __IOS__ || HAS_UNO_WIN32)`.
* New `PlatformServices/NotificationService.Win32.cs` guarded by `#if HAS_UNO_WIN32` (or whichever Uno desktop‑Windows symbol is correct — confirm via Uno docs, see Risk #2).

The Windows implementation:

```csharp
#if HAS_UNO_WIN32
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;

namespace DailyReflection.PlatformServices;

public partial class NotificationService : INotificationService
{
    private const string Tag = "DailyReflectionToast";
    private static readonly object _gate = new();
    private static Timer? _timer;

    public bool IsSupported => true;

    public Task<bool> CanScheduleNotifications() => Task.FromResult(true);

    public Task<bool> TryScheduleDailyNotification(
        DateTime notificationTime,
        bool shouldRequestPermission = true)
    {
        AppNotificationManager.Default.Register();
        ScheduleNextRun(notificationTime);
        return Task.FromResult(true);
    }

    private void ScheduleNextRun(DateTime notificationTime)
    {
        lock (_gate)
        {
            _timer?.Dispose();
            var delay = ComputeDelay(notificationTime);
            _timer = new Timer(_ => Fire(notificationTime), null, delay, Timeout.InfiniteTimeSpan);
        }
    }

    private void Fire(DateTime notificationTime)
    {
        var toast = new AppNotificationBuilder()
            .AddText("Daily Reflection")
            .AddText("Time for the daily reflection!")
            .BuildNotification();
        toast.Tag = Tag;
        AppNotificationManager.Default.Show(toast);
        ScheduleNextRun(notificationTime); // tomorrow
    }

    public void CancelNotifications()
    {
        lock (_gate)
        {
            _timer?.Dispose();
            _timer = null;
            AppNotificationManager.Default.RemoveByTagAsync(Tag).AsTask().GetAwaiter().GetResult();
        }
    }

    public void ShowNotificationSettings() =>
        Process.Start(new ProcessStartInfo("ms-settings:notifications") { UseShellExecute = true });

    private static TimeSpan ComputeDelay(DateTime notificationTime)
    {
        var now = DateTime.Now;
        var target = now.Date.Add(notificationTime.TimeOfDay);
        if (target <= now) target = target.AddDays(1);
        return target - now;
    }
}
#endif
```

Register the toast handler at app startup so the activation callback brings the window forward:

```csharp
// App.xaml.cs OnLaunched, after host build
AppNotificationManager.Default.NotificationInvoked += (_, _) =>
    MainWindow?.DispatcherQueue.TryEnqueue(() => MainWindow.Activate());
AppNotificationManager.Default.Register();
```

Add `<PackageReference Include="Microsoft.Windows.SDK.NET.Ref" />` (or the specific WinAppSDK package) under a `Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'windows'"` group in the csproj. Confirm the exact package name against Uno docs (see Risk #2).

### D. macOS / Linux

`PlatformServices/NotificationService.cs` keeps its no‑op contract. Update `IsSupported`:

```csharp
public bool IsSupported => false;
```

Document in code that this branch covers Skia macOS and Linux X11/framebuffer.

### E. Settings ViewModel guard

```csharp
public bool NotificationsSupported => _notificationService.IsSupported;

// In OnNotificationsEnabledChanged:
if (!NotificationsSupported && value)
{
    SetProperty(ref _notificationsEnabled, false);
    return;
}
```

This makes it physically impossible for a user on an unsupported platform to persist `NotificationsEnabled = true`.

## Risks & open questions

1. **Daemon‑less scheduling.** A `Timer`‑based scheduler only fires while the process is alive. Worth surfacing as a known limitation in the new caption: "...while the app is running" (see Acceptance #4 wording).
2. **Uno desktop‑Windows compile symbol.** `HAS_UNO_WIN32` is illustrative; verify the actual symbol Uno 6.4.58 emits for the Windows desktop sub‑target. May be `WINDOWS` or evaluated via `OperatingSystem.IsWindows()` at runtime — runtime check is acceptable and avoids guessing.
3. **WinAppSDK packaging.** `Microsoft.Windows.AppNotifications` requires the unpackaged SDK bootstrap when running outside MSIX. Add the bootstrap call (`Bootstrap.TryInitialize`) in `Platforms/Desktop/Program.cs` if not already present.
4. **macOS native fallback.** Eventually we want `UNUserNotificationCenter` from a native macOS bundle. Tracked here but not in scope for spec 002.

## Done when

- [x] `INotificationService.IsSupported` added; Uno desktop / Uno Android / Uno iOS / MAUI Android / MAUI iOS implementations all set it correctly.
- [x] Desktop scheduler (`PlatformServices/NotificationService.cs`) timer fires once per day and reschedules; Windows partial logs the firing.
- [x] `ShowNotificationSettings()` launches `ms-settings:notifications` via `Process.Start` on Windows; no-op elsewhere.
- [x] macOS and Linux desktop targets report `IsSupported = false`; the Settings toggle is `IsEnabled="{x:Bind ViewModel.NotificationsSupported}"` and the caption only renders when the platform is unsupported.
- [x] `SettingsViewModel.NotificationsEnabled` setter reverts to `false` when `NotificationsSupported = false` (covered by `Setting_NotificationsEnabled_True_On_Unsupported_Platform_Reverts_To_False`).
- [x] Uno desktop builds clean. Mobile heads recompile against the new interface (the `IsSupported` property additions are the only change required).

### Implementation deviations from the original plan

* **No hard WinAppSDK dependency.** The spec proposed pulling in `Microsoft.Windows.AppNotifications`. Doing so requires the Skia desktop binary to also bootstrap the WinAppSDK runtime, which is a non-trivial dependency for an unpackaged Skia desktop build. The implementation now uses an in-process `Timer` for scheduling and a separate `NotificationService.Windows.cs` partial for the *firing* surface — currently a `Debug.WriteLine`, deliberately structured so swapping in `AppNotificationBuilder` is a one-method change once WinAppSDK packaging is wired in. This keeps the timer/scheduling machinery working today and unblocks the rest of the notification gating, while leaving the toast UI as a follow-up.
* **No `ContentDialog` for permission denial on desktop.** Desktop has no permission flow to prompt; the toggle simply can't be turned on. The Settings caption explains why.
* **No `App.Current.MainWindow` interop in `ShowNotificationSettings`.** The fallback chain is "open Settings or do nothing" — there was no equivalent on desktop platforms in the Xamarin original (it never ran on desktop), so a clean `Process.Start` is sufficient.

### Manual verification still required

* On Windows desktop, set the notification time 1 minute in the future, leave the app running, and confirm the timer fires (Debug.WriteLine output today; toast once WinAppSDK is wired in).
* On macOS and Linux desktop, confirm the toggle is disabled and the caption displays.
* On Android / iOS, confirm `IsSupported = true` (no behaviour change for end users; the property is purely additive).
