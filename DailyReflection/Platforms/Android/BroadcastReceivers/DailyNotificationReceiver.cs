using Android.App;
using Android.Content;
using Android.Media;
using Android.OS;
using AndroidX.Core.App;
using DailyReflection.Core.Constants;
using DailyReflection.PlatformServices;

namespace DailyReflection.Uno.Droid.BroadcastReceivers;

/// <summary>
/// Broadcast receiver that handles the scheduled notification alarm.
///
/// Spec 008 alignment notes (vs. the Xamarin original):
///   10.5.7   SetContentTitle ("Time for the daily reflection!") only — no
///            SetContentText. Matches the original single-line layout.
///   10.5.8   No SetPriority — Xamarin relied on the channel/default.
///   10.5.9   [BroadcastReceiver(Enabled = true)] — Exported left unspecified,
///            matching the original. The receiver has no intent filter, so
///            Android's API 31+ rule that exported receivers must have one
///            doesn't apply.
///   10.5.10  Pending intent activity flags = ClearTop only.
///   10.5.12  Reschedule failure throws (no try/catch swallow). Exceptions
///            surface in logs via the alarm framework.
///   10.5.13  Channel sound is set so Android 8+ honours channel-level sound.
/// </summary>
[BroadcastReceiver(Enabled = true)]
public class DailyNotificationReceiver : BroadcastReceiver
{
    private const string ChannelName = "Daily Reflections";
    private const string ChannelDescription = "The daily reflections channel for notifications.";
    private const int PendingIntentId = 0;

    public const string TitleKey = "title";
    public const string MessageKey = "message";

    private bool _channelInitialized = false;
    private int _messageId = -1;
    private NotificationManager? _manager;

    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context == null)
        {
            return;
        }

        if (!_channelInitialized)
        {
            CreateNotificationChannel(context);
        }

        _messageId++;

        var activityIntent = new Intent(context, typeof(DailyReflection.Uno.Droid.MainActivity));
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
            // 10.5.8 - no SetPriority

        var notification = builder.Build();
        _manager?.Notify(_messageId, notification);

        RescheduleNotification(context);
    }

    private static void RescheduleNotification(Context context)
    {
        var prefs = context.GetSharedPreferences(PreferenceConstants.PreferenceSharedName, FileCreationMode.Private);
        if (prefs == null)
        {
            return;
        }

        var timePref = prefs.GetLong(PreferenceConstants.NotificationTime, 0L);
        if (timePref == 0L)
        {
            return;
        }

        // 10.5.12 - intentionally do not swallow exceptions. Background failures
        // surface in adb logcat / crash reporting just as the Xamarin original did.
        Task.Run(async () =>
        {
            var notificationService = new NotificationService();
            await notificationService.TryScheduleDailyNotification(
                DateTime.FromBinary(timePref),
                shouldRequestPermission: false);
        });
    }

    private void CreateNotificationChannel(Context context)
    {
        _manager = context.GetSystemService(Context.NotificationService) as NotificationManager;

        if (_manager == null)
        {
            return;
        }

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
                    .SetUsage(AudioUsageKind.Notification)!
                    .SetContentType(AudioContentType.Sonification)!
                    .Build());

            _manager.CreateNotificationChannel(channel);
        }

        _channelInitialized = true;
    }
}
