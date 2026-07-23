#if __IOS__
using DailyReflection.Services.Notification;
using Foundation;
using UIKit;
using UserNotifications;

namespace DailyReflection.PlatformServices;

/// <summary>
/// iOS-specific implementation of the notification service.
/// Uses UNUserNotificationCenter for scheduling local notifications.
///
/// Spec 008 alignment notes (vs. the Xamarin original):
///   10.5.2  Sound deliberately unset on the content — Xamarin asks the system
///           default to play (or be silenced) per the user's per-app sound setting.
///   10.5.3  Authorization request limited to Alert; Badge / Sound are deferred
///           until a future setting exposes them.
///   10.5.4  Identifier "1" matches the Xamarin original's MessageId.ToString().
///   10.5.5  CancelNotifications removes pending requests only — keeping the
///           friendlier delivered/badge cleanup as a deliberate divergence is
///           noted in the spec, but parity with the original is the goal.
///   10.5.6  Subtitle = "" matches the Xamarin payload byte-for-byte.
/// </summary>
public partial class NotificationService : INotificationService
{
    private const string NotificationIdentifier = "1";

    public bool IsSupported => true;

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

        CancelNotifications();

        var content = new UNMutableNotificationContent
        {
            Title = "Daily Reflection",
            Subtitle = string.Empty, // 10.5.6
            Body = "Time for the daily reflection!",
            // 10.5.2 - leave Sound unset, matching Xamarin behaviour.
            Badge = 1,
        };

        var time = notificationTime.TimeOfDay;
        var dateComponents = new NSDateComponents
        {
            Hour = time.Hours,
            Minute = time.Minutes,
            Second = 0,
        };

        var trigger = UNCalendarNotificationTrigger.CreateTrigger(dateComponents, repeats: true);
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
        // 10.5.5 - match Xamarin: pending only.
        UNUserNotificationCenter.Current.RemoveAllPendingNotificationRequests();
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
                // Fallback to app settings if notification settings URL is unavailable.
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
            // 10.5.3 - Alert only, matching Xamarin.
            var (granted, _) = await UNUserNotificationCenter.Current.RequestAuthorizationAsync(
                UNAuthorizationOptions.Alert);
            return granted;
        }
        catch
        {
            return false;
        }
    }
}
#endif
