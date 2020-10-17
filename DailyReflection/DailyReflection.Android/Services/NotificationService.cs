using System;
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
using DailyReflection.Droid.BroadcastReceivers;
using DailyReflection.Models;
using DailyReflection.Services;
using Java.Util;
using Xamarin.Essentials;
using Xamarin.Forms;

[assembly: Dependency(typeof(DailyReflection.Droid.Services.NotificationService))]
namespace DailyReflection.Droid.Services
{
	public class NotificationService : INotificationService
	{
        private const int AlarmId = 10000;

        public void ScheduleDailyNotification()
        {
            CancelNotifications();

            var alarmManager = (AlarmManager)Platform.AppContext.GetSystemService(Context.AlarmService);
            alarmManager.SetRepeating(AlarmType.RtcWakeup, GetNotificationTime(), AlarmManager.IntervalDay, GetPendingIntent());
        }

        public void CancelNotifications()
        {
            var alarmManager = (AlarmManager)Platform.AppContext.GetSystemService(Context.AlarmService);
            alarmManager.Cancel(GetPendingIntent());
        }

        private static long GetNotificationTime()
		{
           return (long)Preferences.Get("NotificationTime", DateTime.MinValue).TimeOfDay.TotalMilliseconds;
        }

        private PendingIntent GetPendingIntent()
		{
            var intent = new Intent(Platform.AppContext, typeof(DailyNotificationReceiver));

            return PendingIntent.GetBroadcast(Platform.AppContext, AlarmId, intent, PendingIntentFlags.UpdateCurrent);
        }

	}
}