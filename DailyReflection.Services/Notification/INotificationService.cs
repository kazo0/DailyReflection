using System;
using System.Threading.Tasks;

namespace DailyReflection.Services.Notification;

public interface INotificationService
{
	/// <summary>
	/// Whether the running platform can schedule local notifications at all.
	/// Returns <c>false</c> on platforms with no scheduling primitive (e.g. Skia
	/// macOS / Linux desktop). The Settings UI hides its whole Daily Notifications
	/// section when this is <c>false</c>, so the user is never offered a feature
	/// that won't fire.
	/// </summary>
	bool IsSupported { get; }

	Task<bool> CanScheduleNotifications();
	Task<bool> TryScheduleDailyNotification(DateTime notificationTime, bool shouldRequestPermission = true);
	void CancelNotifications();
	void ShowNotificationSettings();
}
