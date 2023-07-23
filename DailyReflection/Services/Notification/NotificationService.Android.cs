using Android.App;
using Android.Content;
using DailyReflection.BroadcastReceivers;

namespace DailyReflection.Services.Notification
{
	public class NotificationService : INotificationService
	{
		public const string ChannelId = "dailyReflections";

		private const int AlarmId = 10000;

		public async Task<bool> CanScheduleNotifications()
			=> await Permissions.CheckStatusAsync<NotificationPermission>() == PermissionStatus.Granted;


		public async Task<bool> TryScheduleDailyNotification(DateTime notificationTime, bool shouldRequestPermission = true)
		{
			var canScheduleNotifications = await CanScheduleNotifications() || shouldRequestPermission && await RequestNotificationPermission();
			if (!canScheduleNotifications)
			{
				return false;
			}

			CancelNotifications();
			var alarmManager = (AlarmManager)Platform.AppContext.GetSystemService(Context.AlarmService);

			if (OperatingSystem.IsAndroidVersionAtLeast(23))
			{
				alarmManager.SetAndAllowWhileIdle(AlarmType.RtcWakeup, GetNotificationTime(notificationTime), GetPendingIntent());
			}
			else
			{
				alarmManager.Set(AlarmType.RtcWakeup, GetNotificationTime(notificationTime), GetPendingIntent());
			}

			return true;
		}

		private static async Task<bool> RequestNotificationPermission()
		{
			return await App.Current.Dispatcher.DispatchAsync(async () =>
			{
				var permission = await Permissions.RequestAsync<NotificationPermission>();

				return permission == PermissionStatus.Granted;
			});
		}

		public void CancelNotifications()
		{
			var alarmManager = (AlarmManager)Platform.AppContext.GetSystemService(Context.AlarmService);
			alarmManager.Cancel(GetPendingIntent());
		}

		public void ShowNotificationSettings()
		{
			var intent = new Intent();
			intent.SetAction(Android.Provider.Settings.ActionAppNotificationSettings);
			intent.PutExtra(Android.Provider.Settings.ExtraAppPackage, Platform.AppContext.PackageName);
			intent.PutExtra(Android.Provider.Settings.ExtraChannelId, ChannelId);
			intent.SetFlags(ActivityFlags.NewTask);
			Platform.AppContext.StartActivity(intent);
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

			if (OperatingSystem.IsAndroidVersionAtLeast(31))
			{
				return PendingIntent.GetBroadcast(Platform.AppContext, AlarmId, intent, PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);
			}
			else
			{
				return PendingIntent.GetBroadcast(Platform.AppContext, AlarmId, intent, PendingIntentFlags.UpdateCurrent);
			}
		}
	}
}