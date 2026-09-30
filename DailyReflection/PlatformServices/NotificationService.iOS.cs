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
			CancelNotifications();
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
		UIApplication.SharedApplication.InvokeOnMainThread(() => OpenNotificationSettings(_ => { }));
	}

	private static void OpenNotificationSettings(Action<bool> completion)
	{
		var app = UIApplication.SharedApplication;
		using var settingsUrl = new NSUrl(OperatingSystem.IsIOSVersionAtLeast(15, 4)
			? UIApplication.OpenNotificationSettingsUrl
			: UIApplication.OpenSettingsUrlString);
		if (app.CanOpenUrl(settingsUrl))
		{
			app.OpenUrl(settingsUrl, new NSDictionary(), completion);
			return;
		}

		// Fallback to app settings if notification settings URL is unavailable.
		using var appSettingsUrl = new NSUrl(UIApplication.OpenSettingsUrlString);
		if (app.CanOpenUrl(appSettingsUrl))
		{
			app.OpenUrl(appSettingsUrl, new NSDictionary(), completion);
		}
		else
		{
			completion(false);
		}
	}

	private async Task<bool> RequestNotificationPermissionAsync()
	{
		try
		{
			var settings = await UNUserNotificationCenter.Current.GetNotificationSettingsAsync();
			if (settings.AuthorizationStatus == UNAuthorizationStatus.NotDetermined)
			{
				// 10.5.3 - Alert only, matching Xamarin. A first denial simply
				// turns the switch off; offer Settings on the next attempt.
				var (granted, _) = await UNUserNotificationCenter.Current.RequestAuthorizationAsync(
					UNAuthorizationOptions.Alert);
				return granted;
			}

			if (settings.AuthorizationStatus == UNAuthorizationStatus.Denied
				&& !await OfferNotificationSettingsAsync())
			{
				return false;
			}

			// Opening Settings is not permission being granted. Recheck only
			// after returning, before scheduling and persisting the enabled state.
			return await CanScheduleNotifications();
		}
		catch
		{
			return false;
		}
	}

	private static async Task<bool> OfferNotificationSettingsAsync()
	{
		var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		UIApplication.SharedApplication.InvokeOnMainThread(() =>
		{
			try
			{
				var window = (Microsoft.UI.Xaml.Application.Current as App)?.MainWindow;
				var presenter = window is null ? null
					: (global::Uno.UI.Xaml.WindowHelper.GetNativeWindow(window) as UIWindow)?.RootViewController;
				while (presenter?.PresentedViewController is { } presented)
				{
					presenter = presented;
				}

				if (presenter is null)
				{
					completion.TrySetResult(false);
					return;
				}

				var dialog = UIAlertController.Create(
					"Notifications are turned off",
					"Allow notifications for Daily Reflection in Settings. Your daily reminder will turn on when you return.",
					UIAlertControllerStyle.Alert);
				dialog.AddAction(UIAlertAction.Create("Cancel", UIAlertActionStyle.Cancel, _ => completion.TrySetResult(false)));
				dialog.AddAction(UIAlertAction.Create("Open Settings", UIAlertActionStyle.Default, _ => completion.TrySetResult(true)));
				presenter.PresentViewController(dialog, true, null);
			}
			catch (Exception error)
			{
				completion.TrySetException(error);
			}
		});

		return await completion.Task && await ShowNotificationSettingsAndWaitForReturnAsync();
	}

	private static async Task<bool> ShowNotificationSettingsAndWaitForReturnAsync()
	{
		var resumed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		var enteredBackground = false;
		// Observe before opening Settings. An activation from a permission alert
		// must not complete the request; the app must actually leave and return.
		using var backgroundObserver = UIApplication.Notifications.ObserveDidEnterBackground((_, _) => enteredBackground = true);
		using var activeObserver = UIApplication.Notifications.ObserveDidBecomeActive((_, _) =>
		{
			if (enteredBackground)
			{
				resumed.TrySetResult(true);
			}
		});

		UIApplication.SharedApplication.InvokeOnMainThread(() =>
		{
			try
			{
				OpenNotificationSettings(opened =>
				{
					if (!opened)
					{
						resumed.TrySetResult(false);
					}
				});
			}
			catch (Exception error)
			{
				resumed.TrySetException(error);
			}
		});

		return await resumed.Task;
	}
}
#endif
