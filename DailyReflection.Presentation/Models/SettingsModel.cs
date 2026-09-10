using CommunityToolkit.Mvvm.Messaging;
using DailyReflection.Core.Constants;
using DailyReflection.Core.Entities;
using DailyReflection.Data.Models;
using DailyReflection.Services.Clipboard;
using DailyReflection.Services.Notification;
using DailyReflection.Services.Settings;
using DailyReflection.Services.VersionTracking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Reactive;
using Uno.Extensions.Reactive.Messaging;

namespace DailyReflection.Presentation.Models;

/// <summary>
/// MVUX model for the Settings tab. Settings are exposed as two-way-bound
/// <see cref="IState{T}"/>s; persistence and notification scheduling run as
/// <c>ForEach</c> side effects on those states (replacing the CommunityToolkit
/// <c>ObservableProperty</c> changed hooks). Each callback compares the new
/// value against the last-persisted one: a callback carrying the stored value
/// is the subscription's initial replay (or a no-op re-set) and must not
/// trigger side effects — merely starting the app never re-persists values or
/// re-schedules/cancels the daily notification (that is
/// <c>StartupMigrationRunner</c>'s job, matching the Xamarin App.OnStart
/// split). Comparing values — rather than counting callbacks — stays correct
/// even if MVUX coalesces the replay with an immediate update.
/// </summary>
public partial record SettingsModel
{
	private readonly INotificationService _notificationService;
	private readonly ISettingsService _settingsService;
	private readonly IClipboardService _clipboardService;
	private readonly IMessenger _messenger;

	/// <summary>How long the "version copied" toast stays up after <see cref="CopyVersion"/>.</summary>
	public static readonly TimeSpan VersionCopiedToastDuration = TimeSpan.FromSeconds(2.5);

	private bool _lastNotificationsEnabled;
	private DateTime _lastNotificationTime;
	private DateTime _lastSoberDate;
	private SoberTimeDisplayPreference _lastDisplayPreference;
	private bool _lastSecularReadings;

	public SettingsModel(
		INotificationService notificationService,
		ISettingsService settingsService,
		IVersionTrackingService versionTrackingService,
		IClipboardService clipboardService,
		IMessenger messenger)
	{
		_notificationService = notificationService;
		_settingsService = settingsService;
		_clipboardService = clipboardService;
		_messenger = messenger;

		_lastNotificationsEnabled = _settingsService.Get(PreferenceConstants.NotificationsEnabled, false);
		_lastNotificationTime = _settingsService.Get(PreferenceConstants.NotificationTime, DateTime.MinValue);
		_lastSoberDate = _settingsService.Get(PreferenceConstants.SoberDate, DateTime.MinValue);
		_lastDisplayPreference = (SoberTimeDisplayPreference)_settingsService.Get(PreferenceConstants.SoberTimeDisplay, 0);
		_lastSecularReadings = _settingsService.Get(PreferenceConstants.SecularReadings, false);

		var initialNotificationsEnabled = _lastNotificationsEnabled;
		var initialNotificationTime = _lastNotificationTime;
		var initialSoberDate = _lastSoberDate;
		var initialDisplayPreference = _lastDisplayPreference;
		var initialSecularReadings = _lastSecularReadings;

		NotificationsEnabled = State.Value(this, () => initialNotificationsEnabled)
			.ForEach(OnNotificationsEnabledChanged);
		NotificationTime = State.Value(this, () => initialNotificationTime)
			.ForEach(OnNotificationTimeChanged);
		SoberDate = State.Value<SettingsModel, DateTimeOffset?>(this, () => initialSoberDate == DateTime.MinValue ? null : new DateTimeOffset(initialSoberDate))
			.ForEach(OnSoberDateChanged);
		SoberTimeDisplayPreference = State.Value(this, () => initialDisplayPreference)
			.ForEach(OnSoberTimeDisplayPreferenceChanged);
		SecularReadings = State.Value(this, () => initialSecularReadings)
			.ForEach(OnSecularReadingsChanged);

		AppVersion = $"{versionTrackingService.CurrentVersion} ({versionTrackingService.CurrentBuild})";
		VersionCopied = State.Value(this, () => false);
	}

	public IState<bool> NotificationsEnabled { get; }

	/// <summary>
	/// The daily notification time. Only its time of day is meaningful; the
	/// persisted date component is kept stable (spec 006 §B), so a value set
	/// with any date (the TimePicker binding converts back onto today) is
	/// normalised onto the stored date before it is persisted.
	/// </summary>
	public IState<DateTime> NotificationTime { get; }

	/// <summary>
	/// The sober date, typed as the DatePicker binds it (<c>SelectedDate</c>).
	/// Null / None when the user has never picked one (store holds
	/// <see cref="DateTime.MinValue"/>): the picker then has no selection, the
	/// Settings row falls back to today and the Sobriety Time tab hides its
	/// date and period displays. Picks after <see cref="MaxDate"/> are clamped.
	/// </summary>
	public IState<DateTimeOffset?> SoberDate { get; }

	public IState<SoberTimeDisplayPreference> SoberTimeDisplayPreference { get; }

	/// <summary>
	/// Show the secular book's readings instead of the A.A. <i>Daily Reflections</i>
	/// text. After persistence, an MVUX entity message updates the Reflection
	/// tab's reading preference and reloads the selected day.
	/// </summary>
	public IState<bool> SecularReadings { get; }

	/// <summary>
	/// Whether the running platform can fire local notifications. SettingsPage.xaml
	/// binds the Daily Notifications section's <c>Visibility</c> to this, so
	/// unsupported platforms (today: Skia macOS / Linux desktop) are not offered
	/// the feature at all; <see cref="OnNotificationsEnabledChanged"/> still guards
	/// the state itself.
	/// </summary>
	public bool NotificationsSupported => _notificationService.IsSupported;

	/// <summary>Upper bound for the sober date (the DatePicker's <c>MaxYear</c>): today.</summary>
	public DateTimeOffset MaxDate => new(DateTime.Today);

	public List<SoberTimeDisplayPreference> AllSoberTimeDisplayPreferences => Enum.GetValues(typeof(SoberTimeDisplayPreference)).Cast<SoberTimeDisplayPreference>().ToList();

	/// <summary>
	/// Runtime app version sourced from <see cref="IVersionTrackingService"/>
	/// (spec 001 — replaces the hard-coded VersionConstants.VersionNumber).
	/// </summary>
	public string AppVersion { get; }

	/// <summary>
	/// True while the "version copied" toast is showing — set by
	/// <see cref="CopyVersion"/>, cleared after <see cref="VersionCopiedToastDuration"/>.
	/// The Settings page binds the toast's Visibility to it.
	/// </summary>
	public IState<bool> VersionCopied { get; }

	/// <summary>
	/// Copies <see cref="AppVersion"/> to the clipboard and shows the copied toast
	/// (bound as the generated CopyVersion command from the Settings version card).
	/// </summary>
	public async ValueTask CopyVersion(CancellationToken ct = default)
	{
		await _clipboardService.SetTextAsync(AppVersion);
		await VersionCopied.SetAsync(true, ct);
		await Task.Delay(VersionCopiedToastDuration, ct);
		await VersionCopied.SetAsync(false, ct);
	}

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

		// Spec 006 §B — preserve the date component on the persisted DateTime
		// instead of rebasing to today on every time change.
		var time = _lastNotificationTime == DateTime.MinValue
			? value
			: new DateTime(
				_lastNotificationTime.Year, _lastNotificationTime.Month, _lastNotificationTime.Day,
				value.Hour, value.Minute, 0, _lastNotificationTime.Kind);
		if (time != value)
		{
			await NotificationTime.SetAsync(time, ct);
			return;
		}

		_lastNotificationTime = time;
		_settingsService.Set(PreferenceConstants.NotificationTime, time);
		await UpdateNotifications(await NotificationsEnabled, time, ct);
	}

	private async ValueTask OnSoberDateChanged(DateTimeOffset? value, CancellationToken ct)
	{
		if (value is not { } picked)
		{
			return;
		}

		// MaxYear only bounds the picker's year; the sober date cannot be in the future.
		if (picked > MaxDate)
		{
			await SoberDate.SetAsync(MaxDate, ct);
			return;
		}

		var date = picked.Date;
		if (date == _lastSoberDate)
		{
			return;
		}

		_lastSoberDate = date;
		_settingsService.Set(PreferenceConstants.SoberDate, date);
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

	private async ValueTask OnSecularReadingsChanged(bool value, CancellationToken ct)
	{
		if (value == _lastSecularReadings)
		{
			return;
		}

		_lastSecularReadings = value;
		_settingsService.Set(PreferenceConstants.SecularReadings, value);
		_messenger.Send(new EntityMessage<ReadingPreference>(EntityChange.Updated, new(value)));
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
