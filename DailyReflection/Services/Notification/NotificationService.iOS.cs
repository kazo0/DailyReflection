using Foundation;
using UIKit;
using UserNotifications;

namespace DailyReflection.Services.Notification
{
	public class NotificationService : INotificationService
	{
		private const int MessageId = 1;

		public void CancelNotifications()
		{
			UNUserNotificationCenter.Current.RemoveAllPendingNotificationRequests();
		}

		public async Task<bool> CanScheduleNotifications()
		{
			var settings = await UNUserNotificationCenter.Current.GetNotificationSettingsAsync();

			return settings.AuthorizationStatus == UNAuthorizationStatus.Authorized;
		}

		public async Task<bool> TryScheduleDailyNotification(DateTime notificationTime, bool shouldRequestPermission = true)
		{
			var canScheduleNotifications = await CanScheduleNotifications() || (shouldRequestPermission && await RequestNotificationPermission());
			if (!canScheduleNotifications)
			{
				return false;
			}

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

			await UNUserNotificationCenter.Current.AddNotificationRequestAsync(request);

			return true;
		}

		public void ShowNotificationSettings()
		{
			if (OperatingSystem.IsIOSVersionAtLeast(15, minor: 4))
			{
				var settingsUrl = new NSUrl(UIApplication.OpenNotificationSettingsUrl);

				if (UIApplication.SharedApplication.CanOpenUrl(settingsUrl))
				{
					UIApplication.SharedApplication.OpenUrl(settingsUrl, options: default(NSDictionary), completion: null);
				}
			}
		}

		private static async Task<bool> RequestNotificationPermission()
		{
			(var granted, _) = await UNUserNotificationCenter.Current.RequestAuthorizationAsync(UNAuthorizationOptions.Alert);

			return granted;
		}
	}
}