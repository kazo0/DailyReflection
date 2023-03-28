using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DailyReflection.Services.Notification
{
	public interface INotificationService
	{
		Task<bool> CanScheduleNotifications();
		Task<bool> TryScheduleDailyNotification(DateTime notificationTime, bool shouldRequestPermission = true);
		void CancelNotifications();
		void ShowNotificationSettings();
	}
}
