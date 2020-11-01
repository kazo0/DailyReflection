using DailyReflection.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DailyReflection.Services
{
	public interface INotificationService
	{
		void Initialize();
		void ScheduleDailyNotification(DateTime notificationTime);
		void CancelNotifications();
	}
}
