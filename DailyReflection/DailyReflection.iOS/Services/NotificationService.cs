using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DailyReflection.Services;
using Foundation;
using UIKit;
using UserNotifications;
using Xamarin.Essentials;
using Xamarin.Forms;

[assembly: Dependency(typeof(DailyReflection.iOS.Services.NotificationService))]
namespace DailyReflection.iOS.Services
{
	public class NotificationService : INotificationService
	{
		private bool _hasNotificationsPermission;
		private int _messageId = 1;

		public void Initialize()
		{
			UNUserNotificationCenter.Current.RequestAuthorization(UNAuthorizationOptions.Alert, (approved, err) =>
			{
				_hasNotificationsPermission = approved;
			});
		}
		public void CancelNotifications()
		{
			throw new NotImplementedException();
		}

		public void ScheduleDailyNotification()
		{
			if (!_hasNotificationsPermission)
			{
				return;
			}

			var content = new UNMutableNotificationContent()
			{
				Title = "Time for the daily reflection!",
				Subtitle = "",
				Body = "",
				Badge = 1
			};

			var time = Preferences.Get("NotificationTime", DateTime.MinValue).TimeOfDay;

			var dateComponents = new NSDateComponents
			{
				Hour = time.Hours,
				Minute = time.Minutes
			};

			var trigger = UNCalendarNotificationTrigger.CreateTrigger(dateComponents, repeats: true);

			var request = UNNotificationRequest.FromIdentifier(_messageId.ToString(), content, trigger);
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