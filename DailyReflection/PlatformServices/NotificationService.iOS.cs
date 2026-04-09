#if __IOS__
using DailyReflection.Services.Notification;
using Foundation;
using UIKit;
using UserNotifications;

namespace DailyReflection.PlatformServices;

/// <summary>
/// iOS-specific implementation of the notification service.
/// Uses UNUserNotificationCenter for scheduling local notifications.
/// </summary>
public partial class NotificationService : INotificationService
{
    private const string NotificationIdentifier = "DailyReflectionNotification";

    public async Task<bool> CanScheduleNotifications()
    {
        var settings = await UNUserNotificationCenter.Current.GetNotificationSettingsAsync();
        return settings.AuthorizationStatus == UNAuthorizationStatus.Authorized;
    }

    public async Task<bool> TryScheduleDailyNotification(DateTime notificationTime, bool shouldRequestPermission = true)
    {
        var canSchedule = await CanScheduleNotifications();
        
        if (!canSchedule && shouldRequestPermission)
        {
            canSchedule = await RequestNotificationPermissionAsync();
        }

        if (!canSchedule)
        {
            return false;
        }

        // Cancel existing notifications first
        CancelNotifications();

        // Create notification content
        var content = new UNMutableNotificationContent
        {
            Title = "Daily Reflection",
            Body = "Time for the daily reflection!",
            Sound = UNNotificationSound.Default,
            Badge = 1
        };

        // Create trigger based on the notification time
        var time = notificationTime.TimeOfDay;
        var dateComponents = new NSDateComponents
        {
            Hour = time.Hours,
            Minute = time.Minutes,
            Second = 0
        };

        // Create a repeating daily trigger
        var trigger = UNCalendarNotificationTrigger.CreateTrigger(dateComponents, repeats: true);

        // Create the request
        var request = UNNotificationRequest.FromIdentifier(
            NotificationIdentifier,
            content,
            trigger);

        try
        {
            await UNUserNotificationCenter.Current.AddNotificationRequestAsync(request);
            return true;
        }
        catch (NSErrorException)
        {
            return false;
        }
    }

    public void CancelNotifications()
    {
        UNUserNotificationCenter.Current.RemoveAllPendingNotificationRequests();
        
        // Also clear delivered notifications
        UNUserNotificationCenter.Current.RemoveAllDeliveredNotifications();
        
        // Reset badge count
        UIApplication.SharedApplication.InvokeOnMainThread(() =>
        {
            UIApplication.SharedApplication.ApplicationIconBadgeNumber = 0;
        });
    }

    public void ShowNotificationSettings()
    {
        UIApplication.SharedApplication.InvokeOnMainThread(() =>
        {
            var settingsUrl = new NSUrl(UIApplication.OpenNotificationSettingsUrl);

            if (UIApplication.SharedApplication.CanOpenUrl(settingsUrl))
            {
                UIApplication.SharedApplication.OpenUrl(settingsUrl, new NSDictionary(), null);
            }
            else
            {
                // Fallback to app settings if notification settings URL is not available
                var appSettingsUrl = new NSUrl(UIApplication.OpenSettingsUrlString);
                if (UIApplication.SharedApplication.CanOpenUrl(appSettingsUrl))
                {
                    UIApplication.SharedApplication.OpenUrl(appSettingsUrl, new NSDictionary(), null);
                }
            }
        });
    }

    private static async Task<bool> RequestNotificationPermissionAsync()
    {
        try
        {
            var (granted, _) = await UNUserNotificationCenter.Current.RequestAuthorizationAsync(
                UNAuthorizationOptions.Alert | 
                UNAuthorizationOptions.Badge | 
                UNAuthorizationOptions.Sound);

            return granted;
        }
        catch
        {
            return false;
        }
    }
}
#endif
