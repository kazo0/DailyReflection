#if __ANDROID__
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using DailyReflection.Services.Notification;
using DailyReflection.Uno.Droid.BroadcastReceivers;
using Windows.Extensions;
using AndroidApplication = Android.App.Application;

namespace DailyReflection.PlatformServices;

/// <summary>
/// Android-specific implementation of the notification service.
/// Uses AlarmManager to schedule daily notifications.
/// </summary>
public partial class NotificationService : INotificationService
{
	public const string ChannelId = "dailyReflections";

	private const int AlarmId = 10000;

	public bool IsSupported => true;

	public Task<bool> CanScheduleNotifications()
	{
		var context = AndroidApplication.Context;
		// Stay independent of a foreground activity: the alarm receivers also
		// call this check from a background broadcast.
		if (OperatingSystem.IsAndroidVersionAtLeast(33)
			&& ContextCompat.CheckSelfPermission(context, Android.Manifest.Permission.PostNotifications) != Permission.Granted)
		{
			return Task.FromResult(false);
		}

		if (!NotificationManagerCompat.From(context).AreNotificationsEnabled())
		{
			return Task.FromResult(false);
		}

		// A user can block the daily-reminder channel while leaving the app's
		// notification permission granted. A channel not created yet is allowed.
		if (OperatingSystem.IsAndroidVersionAtLeast(26)
			&& context.GetSystemService(Context.NotificationService) is NotificationManager manager
			&& manager.GetNotificationChannel(ChannelId) is { Importance: NotificationImportance.None })
		{
			return Task.FromResult(false);
		}

		return Task.FromResult(true);
	}

	public async Task<bool> TryScheduleDailyNotification(DateTime notificationTime, bool shouldRequestPermission = true)
	{
		var canSchedule = await CanScheduleNotifications();
		if (!canSchedule && shouldRequestPermission)
		{
			await RequestNotificationPermissionAsync();
			// The OS settings are authoritative, including app/channel blocks.
			canSchedule = await CanScheduleNotifications();
		}

		if (!canSchedule)
		{
			CancelNotifications();
			return false;
		}

		CancelNotifications();

		var context = AndroidApplication.Context;
		var alarmManager = context.GetSystemService(Context.AlarmService) as AlarmManager;

		if (alarmManager == null)
		{
			return false;
		}

		var triggerTime = GetNotificationTime(notificationTime);
		var pendingIntent = GetPendingIntent();

		if (pendingIntent == null)
		{
			return false;
		}

		// Spec 009: SetAndAllowWhileIdle is the right primitive for a once-per-day
		// reflection. We deliberately do NOT request SCHEDULE_EXACT_ALARM /
		// USE_EXACT_ALARM:
		//   - the time is user-chosen at minute granularity, not safety-critical;
		//   - exact alarms require Play Store policy answers we don't want to maintain.
		if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
		{
			alarmManager.SetAndAllowWhileIdle(AlarmType.RtcWakeup, triggerTime, pendingIntent);
		}
		else
		{
			alarmManager.Set(AlarmType.RtcWakeup, triggerTime, pendingIntent);
		}

		return true;
	}

	public void CancelNotifications()
	{
		var context = AndroidApplication.Context;
		var alarmManager = context.GetSystemService(Context.AlarmService) as AlarmManager;
		var pendingIntent = GetPendingIntent();

		if (pendingIntent != null)
		{
			alarmManager?.Cancel(pendingIntent);
		}
	}

	public void ShowNotificationSettings()
	{
		var context = AndroidApplication.Context;
		var intent = new Intent();
		if (OperatingSystem.IsAndroidVersionAtLeast(26))
		{
			intent.SetAction(Android.Provider.Settings.ActionAppNotificationSettings);
			intent.PutExtra(Android.Provider.Settings.ExtraAppPackage, context.PackageName);
		}
		else
		{
			intent.SetAction(Android.Provider.Settings.ActionApplicationDetailsSettings);
			intent.SetData(Android.Net.Uri.Parse($"package:{context.PackageName}"));
		}
		intent.SetFlags(ActivityFlags.NewTask);
		context.StartActivity(intent);
	}

	private async Task RequestNotificationPermissionAsync()
	{
		if (OperatingSystem.IsAndroidVersionAtLeast(33)
			&& ContextCompat.CheckSelfPermission(AndroidApplication.Context, Android.Manifest.Permission.PostNotifications) != Permission.Granted)
		{
			await PermissionsHelper.TryGetPermission(
				CancellationToken.None,
				Android.Manifest.Permission.PostNotifications);

			if (await CanScheduleNotifications())
			{
				return;
			}

			var activity = await global::Uno.UI.BaseActivity.GetCurrent(CancellationToken.None);
			if (ActivityCompat.ShouldShowRequestPermissionRationale(activity, Android.Manifest.Permission.PostNotifications))
			{
				// Android can still show its permission prompt on the next attempt.
				return;
			}
		}

		// Android stops prompting after repeated denials. App/channel blocks on
		// older versions also need system settings. Offer a way to recover without
		// treating opening Settings as permission being granted.
		await OfferNotificationSettingsAsync();
	}

	private async Task OfferNotificationSettingsAsync()
	{
		var activity = await global::Uno.UI.BaseActivity.GetCurrent(CancellationToken.None);
		var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		activity.RunOnUiThread(() =>
		{
			var dialog = new AlertDialog.Builder(activity)
				.SetTitle("Notifications are turned off")!
				.SetMessage("Allow notifications for Daily Reflection in Android settings. Your daily reminder will turn on when you return.")!
				.SetPositiveButton("Open Settings", (_, _) => completion.TrySetResult(true))!
				.SetNegativeButton("Cancel", (_, _) => completion.TrySetResult(false))!
				.Create()!;
			dialog.DismissEvent += (_, _) => completion.TrySetResult(false);
			dialog.Show();
		});

		if (await completion.Task)
		{
			await ShowNotificationSettingsAndWaitForReturnAsync(activity);
		}
	}

	private async Task ShowNotificationSettingsAndWaitForReturnAsync(Activity activity)
	{
		var app = Microsoft.UI.Xaml.Application.Current;
		var resumed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		var enteredBackground = false;
		void OnEnteredBackground(object sender, Windows.ApplicationModel.EnteredBackgroundEventArgs args) => enteredBackground = true;
		void OnResuming(object? sender, object args)
		{
			if (enteredBackground)
			{
				resumed.TrySetResult(true);
			}
		}

		// Subscribe before launching Settings, and ignore any pending resume from
		// the permission dialog. Only returning after leaving the app completes
		// this request; TryScheduleDailyNotification then checks the OS again.
		app.EnteredBackground += OnEnteredBackground;
		app.Resuming += OnResuming;
		try
		{
			activity.RunOnUiThread(() =>
			{
				try
				{
					ShowNotificationSettings();
				}
				catch (Exception error)
				{
					resumed.TrySetException(error);
				}
			});
			await resumed.Task;
		}
		finally
		{
			app.EnteredBackground -= OnEnteredBackground;
			app.Resuming -= OnResuming;
		}
	}

	private static long GetNotificationTime(DateTime notificationTime)
	{
		var time = notificationTime.TimeOfDay;
		var alarmDay = DateTime.Now;

		// If the time has already passed today, schedule for tomorrow
		if (alarmDay.TimeOfDay > time)
		{
			alarmDay = alarmDay.AddDays(1);
		}

		var linuxEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		var alarmDate = new DateTime(alarmDay.Year, alarmDay.Month, alarmDay.Day, time.Hours, time.Minutes, time.Seconds)
			.ToUniversalTime();

		return (long)(alarmDate - linuxEpoch).TotalMilliseconds;
	}

	private static PendingIntent? GetPendingIntent()
	{
		var context = AndroidApplication.Context;
		var intent = new Intent(context, typeof(DailyNotificationReceiver));

		var flags = Build.VERSION.SdkInt >= BuildVersionCodes.S
			? PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent
			: PendingIntentFlags.UpdateCurrent;

		return PendingIntent.GetBroadcast(context, AlarmId, intent, flags);
	}
}
#endif
