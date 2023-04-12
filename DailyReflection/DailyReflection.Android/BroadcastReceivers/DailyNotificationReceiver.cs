using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Media;
using Android.OS;
using Android.Preferences;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AndroidX.Core.App;
using DailyReflection.Core.Constants;
using DailyReflection.Droid.Services;
using DailyReflection.Services;
using DailyReflection.Services.Settings;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace DailyReflection.Droid.BroadcastReceivers
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

			if (Build.VERSION.SdkInt >= BuildVersionCodes.S)
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
}