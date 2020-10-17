﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.App;
using Android.Views;
using Android.Widget;
using Xamarin.Essentials;

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
                .SetSmallIcon(Resource.Mipmap.icon_round)
				.SetDefaults((Preferences.Get("NotificationSound", false) ? (int)NotificationDefaults.Sound : 0) | (Preferences.Get("NotificationVibrate", false) ? (int)NotificationDefaults.Vibrate : 0));

			var notification = builder.Build();
			manager.Notify(messageId, notification);
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
                manager.CreateNotificationChannel(channel);
            }

            channelInitialized = true;
        }
    }
}