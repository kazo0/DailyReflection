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

        public void Initialize()
        {
            
        }

        public void ScheduleDailyNotification()
        {
            CancelNotifications();

            var alarmManager = (AlarmManager)Platform.AppContext.GetSystemService(Context.AlarmService);
            if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
            {
                alarmManager.SetExactAndAllowWhileIdle(AlarmType.RtcWakeup, GetNotificationTime(), GetPendingIntent());
            } else
			{
                alarmManager.SetExact(AlarmType.RtcWakeup, GetNotificationTime(), GetPendingIntent());
            }
        }

        public void CancelNotifications()
        {
            var alarmManager = (AlarmManager)Platform.AppContext.GetSystemService(Context.AlarmService);
            alarmManager.Cancel(GetPendingIntent());
        }

        private static long GetNotificationTime()
		{
            var time = Preferences.Get("NotificationTime", DateTime.MinValue).TimeOfDay;
            var alarmDay = DateTime.Now;

            if (alarmDay.TimeOfDay > time)
			{
                alarmDay = alarmDay.AddDays(1);
			}

            var linuxEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var alarmDate = new DateTime(alarmDay.Year, alarmDay.Month, alarmDay.Day, time.Hours, time.Minutes, time.Seconds).ToUniversalTime();


            return (long)(alarmDate - linuxEpoch).TotalMilliseconds;
        }

        private PendingIntent GetPendingIntent()
		{
            var intent = new Intent(Platform.AppContext, typeof(DailyNotificationReceiver));

            return PendingIntent.GetBroadcast(Platform.AppContext, AlarmId, intent, PendingIntentFlags.UpdateCurrent);
        }
	}
}