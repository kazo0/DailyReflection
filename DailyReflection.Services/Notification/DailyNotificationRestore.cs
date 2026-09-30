using System;

namespace DailyReflection.Services.Notification;

/// <summary>
/// Decides what the Android alarm receivers re-arm after a reboot or after a
/// reminder fires, from the values mirrored into SharedPreferences.
/// </summary>
public static class DailyNotificationRestore
{
	/// <summary>
	/// The time to re-arm the daily reminder at, or <c>null</c> when reminders are off.
	/// </summary>
	/// <param name="notificationsEnabled">The stored <c>NotificationsEnabled</c> flag.</param>
	/// <param name="storedNotificationTime">
	/// The stored <c>NotificationTime</c> as <see cref="DateTime.ToBinary"/>, or 0 when missing.
	/// 0 is also <see cref="DateTime.MinValue"/>: the Settings default, which shows as 12:00 AM,
	/// so a user who enabled reminders without touching the time picker has nothing but 0
	/// stored. Treat it as midnight, as <c>StartupMigrationRunner</c> does, not as "not configured".
	/// </param>
	public static DateTime? GetRestoreTime(bool notificationsEnabled, long storedNotificationTime)
		=> notificationsEnabled ? DateTime.FromBinary(storedNotificationTime) : null;
}
