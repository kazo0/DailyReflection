using System;
using System.Collections.Generic;
using System.Text;

namespace DailyReflection.Services.Notification
{
	public interface INotificationService
	{
		void ScheduleDailyNotification(DateTime notificationTime);
		void CancelNotifications();
	}
}
