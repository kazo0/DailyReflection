using Android.App;
using Android.Content;
using DailyReflection.Core.Constants;
using DailyReflection.PlatformServices;

namespace DailyReflection.Uno.Droid.BroadcastReceivers;

/// <summary>
/// Re-schedules the daily reflection notification after device boot or when
/// the SCHEDULE_EXACT_ALARM permission state changes (Android 13+).
/// Without this, alarms set via AlarmManager are dropped on reboot.
/// </summary>
[BroadcastReceiver(Enabled = true, Exported = true)]
[IntentFilter(
    new[] { Intent.ActionBootCompleted, AlarmManager.ActionScheduleExactAlarmPermissionStateChanged },
    Categories = new[] { "android.intent.category.DEFAULT" })]
public class WakeUpAlarmReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context == null)
        {
            return;
        }

        var prefs = context.GetSharedPreferences(PreferenceConstants.PreferenceSharedName, FileCreationMode.Private);
        if (prefs == null)
        {
            return;
        }

        if (!prefs.GetBoolean(PreferenceConstants.NotificationsEnabled, false))
        {
            return;
        }

        var timePref = prefs.GetLong(PreferenceConstants.NotificationTime, 0L);
        if (timePref == 0L)
        {
            return;
        }

        var time = DateTime.FromBinary(timePref);

        Task.Run(async () =>
        {
            try
            {
                var notificationService = new NotificationService();
                await notificationService.TryScheduleDailyNotification(time, shouldRequestPermission: false);
            }
            catch
            {
                // Silently fail if rescheduling fails
            }
        });
    }
}
