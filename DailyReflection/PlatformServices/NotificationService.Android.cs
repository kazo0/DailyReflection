#if __ANDROID__
using Android.App;
using Android.Content;
using Android.OS;
using DailyReflection.Core.Constants;
using DailyReflection.Services.Notification;
using DailyReflection.Uno.Droid.BroadcastReceivers;
using Windows.Extensions;
using AndroidApplication = Android.App.Application;

namespace DailyReflection.PlatformServices;

/// <summary>
/// Android-specific implementation of the notification service.
/// Uses AlarmManager to schedule daily notifications.
/// </summary>
public partial class NotificationService : INotificationService
{
    public const string ChannelId = "dailyReflections";

    private const int AlarmId = 10000;

    public async Task<bool> CanScheduleNotifications()
    {
        if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
        {
            return await PermissionsHelper.CheckPermission(
                CancellationToken.None, 
                Android.Manifest.Permission.PostNotifications);
        }
        
        // Before Android 13, notifications are enabled by default
        return true;
    }

    public async Task<bool> TryScheduleDailyNotification(DateTime notificationTime, bool shouldRequestPermission = true)
    {
        var canSchedule = await CanScheduleNotifications();
        if (!canSchedule && shouldRequestPermission)
        {
            canSchedule = await RequestNotificationPermissionAsync();
        }

        if (!canSchedule)
        {
            return false;
        }

        CancelNotifications();
        
        var context = AndroidApplication.Context;
        var alarmManager = context.GetSystemService(Context.AlarmService) as AlarmManager;
        
        if (alarmManager == null)
        {
            return false;
        }

        var triggerTime = GetNotificationTime(notificationTime);
        var pendingIntent = GetPendingIntent();

        if (pendingIntent == null)
        {
            return false;
        }

        if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
        {
            alarmManager.SetAndAllowWhileIdle(AlarmType.RtcWakeup, triggerTime, pendingIntent);
        }
        else
        {
            alarmManager.Set(AlarmType.RtcWakeup, triggerTime, pendingIntent);
        }

        return true;
    }

    public void CancelNotifications()
    {
        var context = AndroidApplication.Context;
        var alarmManager = context.GetSystemService(Context.AlarmService) as AlarmManager;
        var pendingIntent = GetPendingIntent();
        
        if (pendingIntent != null)
        {
            alarmManager?.Cancel(pendingIntent);
        }
    }

    public void ShowNotificationSettings()
    {
        var context = AndroidApplication.Context;
        var intent = new Intent();
        intent.SetAction(Android.Provider.Settings.ActionAppNotificationSettings);
        intent.PutExtra(Android.Provider.Settings.ExtraAppPackage, context.PackageName);
        intent.PutExtra(Android.Provider.Settings.ExtraChannelId, ChannelId);
        intent.SetFlags(ActivityFlags.NewTask);
        context.StartActivity(intent);
    }

    private static async Task<bool> RequestNotificationPermissionAsync()
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.Tiramisu)
        {
            return true;
        }

        // Use Uno's PermissionsHelper which properly awaits the permission result
        return await PermissionsHelper.TryGetPermission(
            CancellationToken.None, 
            Android.Manifest.Permission.PostNotifications);
    }

    private static long GetNotificationTime(DateTime notificationTime)
    {
        var time = notificationTime.TimeOfDay;
        var alarmDay = DateTime.Now;

        // If the time has already passed today, schedule for tomorrow
        if (alarmDay.TimeOfDay > time)
        {
            alarmDay = alarmDay.AddDays(1);
        }

        var linuxEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var alarmDate = new DateTime(alarmDay.Year, alarmDay.Month, alarmDay.Day, time.Hours, time.Minutes, time.Seconds)
            .ToUniversalTime();

        return (long)(alarmDate - linuxEpoch).TotalMilliseconds;
    }

    private static PendingIntent? GetPendingIntent()
    {
        var context = AndroidApplication.Context;
        var intent = new Intent(context, typeof(DailyNotificationReceiver));

        var flags = Build.VERSION.SdkInt >= BuildVersionCodes.S
            ? PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent
            : PendingIntentFlags.UpdateCurrent;

        return PendingIntent.GetBroadcast(context, AlarmId, intent, flags);
    }
}
#endif
