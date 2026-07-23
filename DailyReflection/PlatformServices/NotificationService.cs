#if !(__ANDROID__ || __IOS__)
using System.Diagnostics;
using System.Threading;
using DailyReflection.Services.Notification;

namespace DailyReflection.PlatformServices;

/// <summary>
/// Desktop notification implementation.
/// <para>
/// On Windows, schedules a once-per-day in-process timer that raises a Windows toast
/// via <c>ToastNotificationManager</c> when available; falls back to logging if the
/// WinAppSDK toast pipeline is not bootstrapped.
/// </para>
/// <para>
/// On macOS and Linux desktop targets there is no system-wide notification scheduler
/// available to a Skia desktop app without a native bundle, so the implementation
/// degrades to <see cref="IsSupported"/> = <c>false</c> and the Settings UI disables
/// the toggle (spec 002 §B).
/// </para>
/// </summary>
public partial class NotificationService : INotificationService
{
	private static readonly object _gate = new();
	private static Timer? _timer;
	private static DateTime _scheduledTime;
	private const string Tag = "DailyReflectionToast";

	/// <summary>
	/// True only on Windows desktop. macOS and Linux remain unsupported until a
	/// native bundle / libnotify path is added (out of scope for spec 002).
	/// </summary>
	public bool IsSupported => OperatingSystem.IsWindows();

	public Task<bool> CanScheduleNotifications() => Task.FromResult(IsSupported);

	public Task<bool> TryScheduleDailyNotification(DateTime notificationTime, bool shouldRequestPermission = true)
	{
		if (!IsSupported)
		{
			return Task.FromResult(false);
		}

		ScheduleNextRun(notificationTime);
		return Task.FromResult(true);
	}

	public void CancelNotifications()
	{
		lock (_gate)
		{
			_timer?.Dispose();
			_timer = null;
		}
	}

	public void ShowNotificationSettings()
	{
		if (!OperatingSystem.IsWindows())
		{
			return;
		}

		try
		{
			Process.Start(new ProcessStartInfo("ms-settings:notifications") { UseShellExecute = true });
		}
		catch
		{
			// best effort — silently swallow on locked-down environments.
		}
	}

	private void ScheduleNextRun(DateTime notificationTime)
	{
		lock (_gate)
		{
			_timer?.Dispose();
			_scheduledTime = notificationTime;
			var delay = ComputeDelay(notificationTime);
			_timer = new Timer(_ => Fire(), null, delay, Timeout.InfiniteTimeSpan);
		}
	}

	private void Fire()
	{
		try
		{
			RaiseToast();
		}
		finally
		{
			ScheduleNextRun(_scheduledTime); // tomorrow
		}
	}

	private static TimeSpan ComputeDelay(DateTime notificationTime)
	{
		var now = DateTime.Now;
		var target = now.Date.Add(notificationTime.TimeOfDay);
		if (target <= now)
		{
			target = target.AddDays(1);
		}
		return target - now;
	}

	// Implemented as partial — the Windows partial raises a real toast.
	// On platforms without a notification surface this is a logging no-op.
	partial void RaiseToast();
}
#endif
