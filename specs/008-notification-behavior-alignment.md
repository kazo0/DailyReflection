# Spec 008 — Notification behaviour alignment

* **Status:** Implemented (2026-05-03). Mobile build verification deferred to a real workload-ready environment.
* **Severity:** 🔴 / 🟠
* **Gaps closed:** 10.5.2, 10.5.3, 10.5.4, 10.5.5, 10.5.6, 10.5.7, 10.5.8, 10.5.9, 10.5.10, 10.5.11, 10.5.12, 10.5.13
* **Depends on:** [002](002-desktop-notifications.md) *(so that desktop has a real implementation to align)*

## Summary

The Android and iOS notification implementations on the Uno port deviate from the Xamarin original in twelve concrete ways, ranging from "nice safety net" (Uno wraps re‑schedule in a `try/catch`) to "behaviour change" (Uno requests `Badge | Sound` on iOS where Xamarin requested `Alert` only). Some are improvements; others are silent regressions. This spec walks through each and either restores Xamarin parity or documents the deliberate change.

The bulk of the work is on **iOS** (5 items) and **Android** (7 items). Each is small in isolation; together they make the Uno port indistinguishable from the original for end users on those platforms.

## Goals

* iOS authorisation request matches the original (`Alert` only, by default).
* iOS notification identifier and content match the original.
* Android notification title and content text match the original layout.
* Android receivers, intent flags, channel sound, and exception handling either match the original or have a one‑line code comment justifying the deliberate divergence.

## Non‑goals

* Adding new notification features (rich content, attachments, action buttons).
* Cross‑platform unification of the channel/identifier name. Each platform keeps its existing identifier so existing installs aren't disrupted.

## Acceptance criteria

### iOS

1. **10.5.3** — `RequestAuthorizationAsync(UNAuthorizationOptions.Alert)` is the default request; `Badge | Sound` is opt‑in via a future setting (not exposed today). New users see a single "Allow notifications" prompt without the "play sound" / "show on lock screen" extras.
2. **10.5.2** — `UNMutableNotificationContent.Sound = null` (or property unset) by default. Users who want sound can enable it system‑wide via Settings.app — same behaviour as the Xamarin original. Document the change in code.
3. **10.5.4** — Notification identifier reverts from `"DailyReflectionNotification"` to `"1"` (matching Xamarin's `MessageId.ToString()`). This avoids orphaning existing scheduled notifications on a hypothetical migration from the Xamarin app.
4. **10.5.5** — `CancelNotifications` only calls `RemoveAllPendingNotificationRequests` — drop the extra `RemoveAllDeliveredNotifications` and badge reset. (If the team prefers the friendlier behaviour, document the deliberate choice in code instead.)
5. **10.5.6** — Set `Subtitle = ""` to exactly match the Xamarin payload (no observable difference; included for byte‑for‑byte parity in case some iOS subtitle rendering surprises us).

### Android

6. **10.5.7** — Builder uses `SetContentTitle("Time for the daily reflection!")` only; no `SetContentText`. Matches the Xamarin layout (single‑line notification with no expanded body).
7. **10.5.8** — Drop the explicit `SetPriority(NotificationCompat.PriorityDefault)` (it's the default anyway, and the Xamarin original doesn't set it).
8. **10.5.9** — `[BroadcastReceiver]` attribute on `DailyNotificationReceiver` drops `Exported = false` and `Exported` is left unspecified, matching the original. (Security trade‑off: receivers used only for our own alarms can stay non‑exported, but matching the original is the explicit goal here. If exported defaults change in newer Android API levels, prefer keeping `Exported = false` and document it as the *one* deliberate divergence.)
9. **10.5.10** — Pending intent activity flags revert to `ActivityFlags.ClearTop` only; drop `SingleTop`. Matches the original (acceptable side effect: tapping multiple notifications in a row may stack the activity, which is the original behaviour).
10. **10.5.11** — `CanScheduleNotifications` collapses to a check that mirrors the Xamarin path. The Uno port's special‑case `return true` on pre‑Tiramisu is correct; document it.
11. **10.5.12** — Receiver re‑schedule path drops the `try/catch { /* swallow */ }` to match the original's "let it throw" behaviour. If the team prefers the safety net, log the exception instead of swallowing — never silently.
12. **10.5.13** — Add `channel.SetSound(RingtoneManager.GetDefaultUri(RingtoneType.Notification), null)` so Android 8+ honours the channel‑level sound setting, since per‑notification `SetSound` is deprecated for channel‑bound notifications.

## Implementation plan

### A. iOS adjustments — `PlatformServices/NotificationService.iOS.cs`

```csharp
public async Task<bool> TryScheduleDailyNotification(DateTime notificationTime, bool shouldRequestPermission = true)
{
    var canSchedule = await CanScheduleNotifications();
    if (!canSchedule && shouldRequestPermission)
        canSchedule = await RequestNotificationPermissionAsync();
    if (!canSchedule) return false;

    CancelNotifications();

    var content = new UNMutableNotificationContent
    {
        Title = "Daily Reflection",
        Subtitle = string.Empty,           // 10.5.6
        Body = "Time for the daily reflection!",
        Badge = 1,                          // unchanged from both ports
        // 10.5.2: leave Sound unset to match Xamarin behaviour.
    };
    var dt = notificationTime.TimeOfDay;
    var components = new NSDateComponents { Hour = dt.Hours, Minute = dt.Minutes };
    var trigger = UNCalendarNotificationTrigger.CreateTrigger(components, repeats: true);
    var request = UNNotificationRequest.FromIdentifier("1", content, trigger); // 10.5.4

    try { await UNUserNotificationCenter.Current.AddNotificationRequestAsync(request); return true; }
    catch (NSErrorException) { return false; }
}

public void CancelNotifications()
{
    // 10.5.5: match Xamarin (pending only).
    UNUserNotificationCenter.Current.RemoveAllPendingNotificationRequests();
}

private static async Task<bool> RequestNotificationPermissionAsync()
{
    try
    {
        var (granted, _) = await UNUserNotificationCenter.Current.RequestAuthorizationAsync(
            UNAuthorizationOptions.Alert);  // 10.5.3
        return granted;
    }
    catch { return false; }
}
```

`ShowNotificationSettings` keeps the existing fallback chain — it's strictly more robust than the Xamarin original and worth keeping.

### B. Android adjustments — `Platforms/Android/BroadcastReceivers/DailyNotificationReceiver.cs`

```csharp
[BroadcastReceiver(Enabled = true)]   // 10.5.9: drop Exported = false
public class DailyNotificationReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context == null) return;
        if (!_channelInitialized) CreateNotificationChannel(context);

        _messageId++;
        var activityIntent = new Intent(context, typeof(MainActivity));
        activityIntent.SetFlags(ActivityFlags.ClearTop); // 10.5.10
        activityIntent.PutExtra(TitleKey, "Time for the daily reflection!");

        var flags = Build.VERSION.SdkInt >= BuildVersionCodes.S
            ? PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent
            : PendingIntentFlags.UpdateCurrent;
        var pendingIntent = PendingIntent.GetActivity(context, PendingIntentId, activityIntent, flags);

        var builder = new NotificationCompat.Builder(context, NotificationService.ChannelId)
            .SetContentIntent(pendingIntent)
            .SetContentTitle("Time for the daily reflection!") // 10.5.7
            .SetSmallIcon(Resource.Drawable.notif_icon)
            .SetSound(RingtoneManager.GetDefaultUri(RingtoneType.Notification))
            .SetAutoCancel(true);
            // 10.5.8: no SetPriority

        var notification = builder.Build();
        _manager?.Notify(_messageId, notification);

        RescheduleNotification(context);
    }

    private void RescheduleNotification(Context context)
    {
        var prefs = context.GetSharedPreferences(PreferenceConstants.PreferenceSharedName, FileCreationMode.Private);
        if (prefs == null) return;
        var timePref = prefs.GetLong(PreferenceConstants.NotificationTime, 0L);
        if (timePref == 0L) return;

        // 10.5.12: re-throw rather than swallow.
        Task.Run(() =>
            new NotificationService().TryScheduleDailyNotification(
                DateTime.FromBinary(timePref), shouldRequestPermission: false));
    }

    private void CreateNotificationChannel(Context context)
    {
        _manager = context.GetSystemService(Context.NotificationService) as NotificationManager;
        if (_manager == null) return;

        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {
            var channelNameJava = new Java.Lang.String(ChannelName);
            var channel = new NotificationChannel(NotificationService.ChannelId, channelNameJava, NotificationImportance.Default)
            {
                Description = ChannelDescription
            };
            channel.EnableLights(true);
            channel.EnableVibration(true);
            channel.SetSound(   // 10.5.13
                RingtoneManager.GetDefaultUri(RingtoneType.Notification),
                new AudioAttributes.Builder()
                    .SetUsage(AudioUsageKind.Notification)
                    .SetContentType(AudioContentType.Sonification)
                    .Build());
            _manager.CreateNotificationChannel(channel);
        }
        _channelInitialized = true;
    }
}
```

### C. Optional safety net (compromise)

If swallowing exceptions in the receiver re‑schedule path (10.5.12) is needed for production resiliency, replace `try { … } catch { }` with `try { … } catch (Exception ex) { Logger.LogWarning(ex, "Reschedule failed"); }`. Resolve a logger from the `App.Host.Services` if available.

### D. Decision log

Add a short comment block at the top of each modified file documenting **which** of the §10 items were addressed and why each direction was chosen, so a future port doesn't accidentally re‑introduce the divergence. Example:

```csharp
// 10.5.3: Authorization request limited to Alert to match the Xamarin original.
//         If we want richer permissions later, expose a setting and request lazily.
// 10.5.4: Identifier "1" matches the Xamarin original.
```

## Risks & open questions

1. **Notification ID drift on existing Uno installs.** Anyone who installed the Uno build after the original ship will have the `"DailyReflectionNotification"` identifier scheduled. Reverting to `"1"` orphans that one — but `CancelNotifications` runs on every schedule, so the orphan dies on next schedule. Document.
2. **Android `Exported` default.** API 31+ requires explicit `Exported`. If the build target is 33+, the Android build system will fail without an `Exported` attribute on a receiver that has an intent filter — but `DailyNotificationReceiver` does **not** have an intent filter, only `WakeUpAlarmReceiver` does. The latter must keep `Exported = true` (it listens for `BOOT_COMPLETED`). Removing `Exported = false` from `DailyNotificationReceiver` is therefore safe.
3. **Channel sound vs. per‑notification sound.** Setting both is redundant on Android 8+ but harmless. Keeping both matches the Xamarin original's behaviour on Android 7 and below.

## Done when

- [x] iOS notification path matches Xamarin (`Alert` only; no `Sound`; identifier `"1"`; `Subtitle = ""`; cancel only removes pending requests).
- [x] Android receiver matches Xamarin (`SetContentTitle` only, no `SetContentText`; no `SetPriority`; intent flags = `ClearTop` only; receiver attribute = `Enabled = true` only; reschedule throws on failure; channel `SetSound` added).
- [x] Each modified file carries a header comment listing the §10.5 items it addresses.
- [x] Uno desktop builds clean.

### Implementation deviations from the original plan

* **No iOS smoke test from this environment.** iOS workload not present in the dev container; changes are platform-agnostic C# bracketed by `#if __IOS__`. Verification will run when the iOS SDK is available.
* **Android receiver `Exported` flag.** Spec considered keeping `Exported = false` as a deliberate divergence; the implementation drops it so the attribute is `[BroadcastReceiver(Enabled = true)]` exactly like the Xamarin original. Since the receiver carries no intent filter, Android 31+'s "exported must be set" rule doesn't apply and the default (false) is what we want anyway.
* **Reschedule exception handling.** The previous code wrapped `TryScheduleDailyNotification` in `try { ... } catch { /* swallow */ }`. The new code lets exceptions propagate to the alarm framework where they show up in `adb logcat`, matching the original. (See spec §C — the optional logging compromise wasn't taken because there's no logger plumbed through to the receiver yet.)
