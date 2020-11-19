using DailyReflection.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DailyReflection.Services
{
	public interface INotificationService
	{
		void ScheduleDailyNotification(DateTime notificationTime);
		void CancelNotifications();
	}
}
