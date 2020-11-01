using System;
using Android.App;
using Android.Content;
using Android.OS;
using DailyReflection.Droid.BroadcastReceivers;
using DailyReflection.Services;
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

        public void ScheduleDailyNotification(DateTime notificationTime)
        {
            CancelNotifications();

            var alarmManager = (AlarmManager)Platform.AppContext.GetSystemService(Context.AlarmService);
            if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
            {
                alarmManager.SetExactAndAllowWhileIdle(AlarmType.RtcWakeup, GetNotificationTime(notificationTime), GetPendingIntent());
            } else
			{
                alarmManager.SetExact(AlarmType.RtcWakeup, GetNotificationTime(notificationTime), GetPendingIntent());
            }
        }

        public void CancelNotifications()
        {
            var alarmManager = (AlarmManager)Platform.AppContext.GetSystemService(Context.AlarmService);
            alarmManager.Cancel(GetPendingIntent());
        }

        private static long GetNotificationTime(DateTime notificationTime)
		{
            var time = notificationTime.TimeOfDay;
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