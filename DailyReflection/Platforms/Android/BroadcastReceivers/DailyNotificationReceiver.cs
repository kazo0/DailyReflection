using Android.App;
using Android.Content;
using Android.Media;
using AndroidX.Core.App;
using AndroidX.Core.Graphics.Drawable;
using DailyReflection.Constants;
using DailyReflection.Services.Notification;

namespace DailyReflection.BroadcastReceivers
{
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
		private NotificationManager _manager;

		public override void OnReceive(Context context, Intent intent)
		{
			if (!_channelInitialized)
			{
				CreateNotificationChannel();
			}

			_messageId++;

			Intent notifIntent = new Intent(Platform.AppContext, typeof(MainActivity));

			notifIntent.SetFlags(ActivityFlags.ClearTop);
			notifIntent.PutExtra(TitleKey, "Time for the daily reflection!");

			PendingIntent pendingIntent;

			if (OperatingSystem.IsAndroidVersionAtLeast(31))
			{
				pendingIntent = PendingIntent.GetActivity(Platform.AppContext, PendingIntentId, notifIntent, PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);
			}
			else
			{
				pendingIntent = PendingIntent.GetActivity(Platform.AppContext, PendingIntentId, notifIntent, PendingIntentFlags.UpdateCurrent);
			}

			NotificationCompat.Builder builder = new NotificationCompat.Builder(Platform.AppContext, NotificationService.ChannelId)
				.SetContentIntent(pendingIntent)
				.SetContentTitle("Time for the daily reflection!")
				.SetSmallIcon(Resource.Drawable.notif_icon)
				.SetSound(RingtoneManager.GetDefaultUri(RingtoneType.Notification))
				.SetAutoCancel(true);

			var notification = builder.Build();
			_manager.Notify(_messageId, notification);

			var prefs = context.GetSharedPreferences(PreferenceConstants.PreferenceSharedName, FileCreationMode.Private);
			if (prefs == null)
			{
				return;
			}

			var timePref = prefs.GetLong(PreferenceConstants.NotificationTime, 0L);

			Task.Run(() => _ = new NotificationService().TryScheduleDailyNotification(DateTime.FromBinary(timePref), shouldRequestPermission: false));
		}

		private void CreateNotificationChannel()
		{
			_manager = (NotificationManager)Platform.AppContext.GetSystemService(Context.NotificationService);

			if (OperatingSystem.IsAndroidVersionAtLeast(26))
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
}