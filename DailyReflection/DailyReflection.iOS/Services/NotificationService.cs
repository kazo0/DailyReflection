﻿using DailyReflection.Services;
using Foundation;
using System;
using UserNotifications;
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
			UNUserNotificationCenter.Current.RemoveAllPendingNotificationRequests();
		}

		public void ScheduleDailyNotification(DateTime notificationTime)
		{
			if (!_hasNotificationsPermission)
			{
				return;
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

			var request = UNNotificationRequest.FromIdentifier(_messageId.ToString(), content, trigger);

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