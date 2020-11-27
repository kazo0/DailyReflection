using DailyReflection.Services.Notification;
using Foundation;
using System;
using UserNotifications;

namespace DailyReflection.iOS.Services
{
	public class NotificationService : INotificationService
	{
		private const int MessageId = 1;

		public void CancelNotifications()
		{
			UNUserNotificationCenter.Current.RemoveAllPendingNotificationRequests();
		}

		public void ScheduleDailyNotification(DateTime notificationTime)
		{
			var content = new UNMutableNotificationContent()
			{
				Title = "Daily Reflection",
				Subtitle = "",
				Body = "Time for the daily reflection!",
				Badge = 1
			};

			var time = notificationTime.TimeOfDay;

			var dateComponents = new NSDateComponents
			{
				Hour = time.Hours,
				Minute = time.Minutes
			};

			var trigger = UNCalendarNotificationTrigger.CreateTrigger(dateComponents, repeats: true);

			var request = UNNotificationRequest.FromIdentifier(MessageId.ToString(), content, trigger);

			CancelNotifications();

			UNUserNotificationCenter.Current.AddNotificationRequest(request, (err) =>
			{
				if (err != null)
				{
					throw new Exception($"Failed to schedule notification: {err}");
				}
			});
		}
	}
}