using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Media;
using Android.OS;
using Android.Preferences;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AndroidX.Core.App;
using DailyReflection.Services;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace DailyReflection.Droid.BroadcastReceivers
{
	[BroadcastReceiver(Enabled = true)]
	public class DailyNotificationReceiver : BroadcastReceiver
	{
        private const string ChannelId = "dailyReflections";
        private const string ChannelName = "Daily Reflections";
        private const string ChannelDescription = "The daily reflections channel for notifications.";
        private const int PendingIntentId = 0;


        public const string TitleKey = "title";
        public const string MessageKey = "message";

        bool channelInitialized = false;
        int messageId = -1;
        NotificationManager manager;

        public override void OnReceive(Context context, Intent intent)
		{
			if (!channelInitialized)
			{
				CreateNotificationChannel();
			}

			messageId++;

			Intent notifIntent = new Intent(Platform.AppContext, typeof(MainActivity));

            notifIntent.SetFlags(ActivityFlags.ClearTop);
			notifIntent.PutExtra(TitleKey, "Time for the daily reflection!");

			PendingIntent pendingIntent = PendingIntent.GetActivity(Platform.AppContext, PendingIntentId, notifIntent, PendingIntentFlags.UpdateCurrent);

            NotificationCompat.Builder builder = new NotificationCompat.Builder(Platform.AppContext, ChannelId)
                .SetContentIntent(pendingIntent)
                .SetContentTitle("Time for the daily reflection!")
                .SetSmallIcon(Resource.Drawable.notif_icon)
                .SetSound(RingtoneManager.GetDefaultUri(RingtoneType.Notification))
                .SetAutoCancel(true);

            var notification = builder.Build();
			manager.Notify(messageId, notification);

            var prefs = PreferenceManager.GetDefaultSharedPreferences(context);
            var timePref = prefs.GetLong("NotificationTime", 0L);
            
            DependencyService.Get<INotificationService>().ScheduleDailyNotification(DateTime.FromBinary(timePref));
		}

        private void CreateNotificationChannel()
        {
            manager = (NotificationManager)Platform.AppContext.GetSystemService(Context.NotificationService);

            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var channelNameJava = new Java.Lang.String(ChannelName);
                var channel = new NotificationChannel(ChannelId, channelNameJava, NotificationImportance.Default)
                {
                    Description = ChannelDescription
                };
                channel.EnableLights(true);
                channel.EnableVibration(true);

                manager.CreateNotificationChannel(channel);
            }

            channelInitialized = true;
        }
    }
}