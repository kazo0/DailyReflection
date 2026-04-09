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
/// Creates and displays the notification when triggered.
/// </summary>
[BroadcastReceiver(Enabled = true, Exported = false)]
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
        activityIntent.SetFlags(ActivityFlags.ClearTop | ActivityFlags.SingleTop);
        activityIntent.PutExtra(TitleKey, "Time for the daily reflection!");

        PendingIntent? pendingIntent;
        var flags = Build.VERSION.SdkInt >= BuildVersionCodes.S
            ? PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent
            : PendingIntentFlags.UpdateCurrent;

        pendingIntent = PendingIntent.GetActivity(context, PendingIntentId, activityIntent, flags);

        var builder = new NotificationCompat.Builder(context, NotificationService.ChannelId)
            .SetContentIntent(pendingIntent)
            .SetContentTitle("Daily Reflection")
            .SetContentText("Time for the daily reflection!")
            .SetSmallIcon(Android.Resource.Drawable.IcDialogInfo) // Use system icon as fallback
            .SetSound(RingtoneManager.GetDefaultUri(RingtoneType.Notification))
            .SetAutoCancel(true)
            .SetPriority(NotificationCompat.PriorityDefault);

        var notification = builder.Build();
        _manager?.Notify(_messageId, notification);

        // Reschedule the next notification
        RescheduleNotification(context);
    }

    private void RescheduleNotification(Context context)
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

        Task.Run(async () =>
        {
            try
            {
                var notificationService = new NotificationService();
                await notificationService.TryScheduleDailyNotification(
                    DateTime.FromBinary(timePref), 
                    shouldRequestPermission: false);
            }
            catch
            {
                // Silently fail if rescheduling fails
            }
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

            _manager.CreateNotificationChannel(channel);
        }

        _channelInitialized = true;
    }
}
