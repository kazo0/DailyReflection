#if !(__ANDROID__ || __IOS__)
using DailyReflection.Services.Notification;

namespace DailyReflection.PlatformServices;

/// <summary>
/// Desktop/fallback notification service implementation.
/// Local notifications are not supported on desktop platforms in Uno Platform.
/// This provides a no-op implementation for non-mobile platforms.
/// </summary>
public partial class NotificationService : INotificationService
{
    public Task<bool> CanScheduleNotifications()
    {
        // Desktop platforms don't support local notifications
        return Task.FromResult(false);
    }

    public void CancelNotifications()
    {
        // No-op for desktop platforms
    }

    public void ShowNotificationSettings()
    {
        // No-op for desktop platforms
    }

    public Task<bool> TryScheduleDailyNotification(DateTime notificationTime, bool shouldRequestPermission = true)
    {
        // Desktop platforms don't support local notifications
        return Task.FromResult(false);
    }
}
#endif
