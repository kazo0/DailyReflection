using System;
using System.Threading.Tasks;

namespace DailyReflection.Services.Notification;

public interface INotificationService
{
	/// <summary>
	/// Whether the running platform can schedule local notifications at all.
	/// Returns <c>false</c> on platforms with no scheduling primitive (e.g. Skia
	/// macOS / Linux desktop). The Settings UI binds <c>ToggleSwitch.IsEnabled</c>
	/// to this so the user cannot turn on a feature that won't fire.
	/// </summary>
	bool IsSupported { get; }

	Task<bool> CanScheduleNotifications();
	Task<bool> TryScheduleDailyNotification(DateTime notificationTime, bool shouldRequestPermission = true);
	void CancelNotifications();
	void ShowNotificationSettings();
}
