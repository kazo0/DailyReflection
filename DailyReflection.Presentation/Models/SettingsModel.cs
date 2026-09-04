using DailyReflection.Core.Constants;
using DailyReflection.Data.Models;
using DailyReflection.Services.Notification;
using DailyReflection.Services.Settings;
using DailyReflection.Services.Theme;
using DailyReflection.Services.VersionTracking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Reactive;

namespace DailyReflection.Presentation.Models;

/// <summary>
/// MVUX model for the Settings tab. Settings are exposed as two-way-bound
/// <see cref="IState{T}"/>s; persistence, notification scheduling and theme
/// switching run as <c>ForEach</c> side effects on those states (replacing the CommunityToolkit
/// <c>ObservableProperty</c> changed hooks). Each callback compares the new
/// value against the last-persisted one: a callback carrying the stored value
/// is the subscription's initial replay (or a no-op re-set) and must not
/// trigger side effects — merely starting the app never re-persists values,
/// re-schedules/cancels the daily notification (that is
/// <c>StartupMigrationRunner</c>'s job, matching the Xamarin App.OnStart
/// split) or re-applies the theme (<c>App.OnLaunched</c> does that once the
/// window has content). Comparing values — rather than counting callbacks — stays correct
/// even if MVUX coalesces the replay with an immediate update.
/// </summary>
public partial record SettingsModel
{
	private readonly INotificationService _notificationService;
	private readonly ISettingsService _settingsService;
	private readonly IAppThemeService _themeService;

	private bool _lastNotificationsEnabled;
	private DateTime _lastNotificationTime;
	private DateTime _lastSoberDate;
	private SoberTimeDisplayPreference _lastDisplayPreference;
	private AppThemePreference _lastThemePreference;

	public SettingsModel(
		INotificationService notificationService,
		ISettingsService settingsService,
		IVersionTrackingService versionTrackingService,
		IAppThemeService themeService)
	{
		_notificationService = notificationService;
		_settingsService = settingsService;
		_themeService = themeService;

		_lastNotificationsEnabled = _settingsService.Get(PreferenceConstants.NotificationsEnabled, false);
		_lastNotificationTime = _settingsService.Get(PreferenceConstants.NotificationTime, DateTime.MinValue);
		_lastSoberDate = _settingsService.Get(PreferenceConstants.SoberDate, DateTime.MinValue);
		_lastDisplayPreference = (SoberTimeDisplayPreference)_settingsService.Get(PreferenceConstants.SoberTimeDisplay, 0);
		_lastThemePreference = (AppThemePreference)_settingsService.Get(PreferenceConstants.AppThemePreference, 0);

		var initialNotificationsEnabled = _lastNotificationsEnabled;
		var initialNotificationTime = _lastNotificationTime;
		var initialSoberDate = _lastSoberDate;
		var initialDisplayPreference = _lastDisplayPreference;
		var initialThemePreference = _lastThemePreference;

		NotificationsEnabled = State.Value(this, () => initialNotificationsEnabled)
			.ForEach(OnNotificationsEnabledChanged);
		NotificationTime = State.Value(this, () => initialNotificationTime)
			.ForEach(OnNotificationTimeChanged);
		SoberDate = State.Value(this, () => initialSoberDate == DateTime.MinValue ? (DateTime?)null : initialSoberDate)
			.ForEach(OnSoberDateChanged);
		SoberTimeDisplayPreference = State.Value(this, () => initialDisplayPreference)
			.ForEach(OnSoberTimeDisplayPreferenceChanged);
		AppThemePreference = State.Value(this, () => initialThemePreference)
			.ForEach(OnAppThemePreferenceChanged);

		AppVersion = $"{versionTrackingService.CurrentVersion} ({versionTrackingService.CurrentBuild})";
	}

	public IState<bool> NotificationsEnabled { get; }

	public IState<DateTime> NotificationTime { get; }

	/// <summary>
	/// The sober date. Has no value (None) when the user has never picked one
	/// (store holds <see cref="DateTime.MinValue"/>). The Sobriety Time tab
	/// hides its date and period displays in that case; the Settings row and
	/// date picker fall back to today.
	/// </summary>
	public IState<DateTime> SoberDate { get; }

	public IState<SoberTimeDisplayPreference> SoberTimeDisplayPreference { get; }

	/// <summary>
	/// The user's theme choice (Settings → Display → Theme). Persisted like the
	/// other settings; the side effect hands the value to <see cref="IAppThemeService"/>,
	/// which re-themes the running window. Defaults to following the OS.
	/// </summary>
	public IState<AppThemePreference> AppThemePreference { get; }

	/// <summary>
	/// Whether the running platform can fire local notifications. Bound to
	/// <c>ToggleSwitch.IsEnabled</c> in SettingsPage.xaml so unsupported
	/// platforms (today: Skia macOS / Linux desktop) cannot toggle the feature on.
	/// </summary>
	public bool NotificationsSupported => _notificationService.IsSupported;

	public DateTime MaxDate => DateTime.Today;

	public List<SoberTimeDisplayPreference> AllSoberTimeDisplayPreferences => Enum.GetValues(typeof(SoberTimeDisplayPreference)).Cast<SoberTimeDisplayPreference>().ToList();

	public List<AppThemePreference> AllAppThemePreferences => Enum.GetValues(typeof(AppThemePreference)).Cast<AppThemePreference>().ToList();

	/// <summary>
	/// Runtime app version sourced from <see cref="IVersionTrackingService"/>
	/// (spec 001 — replaces the hard-coded VersionConstants.VersionNumber).
	/// </summary>
	public string AppVersion { get; }

	private async ValueTask OnNotificationsEnabledChanged(bool value, CancellationToken ct)
	{
		if (value == _lastNotificationsEnabled)
		{
			return;
		}

		// Guard: never persist NotificationsEnabled = true on a platform where
		// notifications cannot fire. Forces the toggle back to false.
		if (value && !NotificationsSupported)
		{
			await NotificationsEnabled.SetAsync(false, ct);
			return;
		}

		_lastNotificationsEnabled = value;
		_settingsService.Set(PreferenceConstants.NotificationsEnabled, value);
		await UpdateNotifications(value, _lastNotificationTime, ct);
	}

	private async ValueTask OnNotificationTimeChanged(DateTime value, CancellationToken ct)
	{
		if (value == _lastNotificationTime)
		{
			return;
		}

		_lastNotificationTime = value;
		_settingsService.Set(PreferenceConstants.NotificationTime, value);
		await UpdateNotifications(await NotificationsEnabled, value, ct);
	}

	private async ValueTask OnSoberDateChanged(DateTime value, CancellationToken ct)
	{
		if (value == _lastSoberDate)
		{
			return;
		}

		_lastSoberDate = value;
		_settingsService.Set(PreferenceConstants.SoberDate, value);
		await Task.CompletedTask;
	}

	private async ValueTask OnSoberTimeDisplayPreferenceChanged(SoberTimeDisplayPreference value, CancellationToken ct)
	{
		if (value == _lastDisplayPreference)
		{
			return;
		}

		_lastDisplayPreference = value;
		_settingsService.Set(PreferenceConstants.SoberTimeDisplay, (int)value);
		await Task.CompletedTask;
	}

	private async ValueTask OnAppThemePreferenceChanged(AppThemePreference value, CancellationToken ct)
	{
		if (value == _lastThemePreference)
		{
			return;
		}

		_lastThemePreference = value;
		_settingsService.Set(PreferenceConstants.AppThemePreference, (int)value);
		_themeService.ApplyTheme(value);
		await Task.CompletedTask;
	}

	private async Task UpdateNotifications(bool enabled, DateTime time, CancellationToken ct)
	{
		if (enabled)
		{
			await _notificationService.TryScheduleDailyNotification(time);
		}
		else
		{
			_notificationService.CancelNotifications();
		}
	}
}
