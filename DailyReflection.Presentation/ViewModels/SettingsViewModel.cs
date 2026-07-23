using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using DailyReflection.Core.Constants;
using DailyReflection.Data.Models;
using DailyReflection.Presentation.Messages;
using DailyReflection.Services.Notification;
using DailyReflection.Services.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace DailyReflection.Presentation.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
	private readonly INotificationService _notificationService;
	private readonly ISettingsService _settingsService;

	[ObservableProperty]
	private bool _notificationsEnabled;

	[ObservableProperty]
	private DateTime _soberDate;

	[ObservableProperty]
	private DateTime _notificationTime;

	[ObservableProperty]
	private SoberTimeDisplayPreference _soberTimeDisplayPreference;

	public DateTime MaxDate => DateTime.Today;

	/// <summary>
	/// Whether the running platform can fire local notifications. Bound to
	/// <c>ToggleSwitch.IsEnabled</c> in <c>SettingsPage.xaml</c> so unsupported
	/// platforms (today: Skia macOS / Linux desktop) cannot toggle the feature on.
	/// </summary>
	public bool NotificationsSupported => _notificationService.IsSupported;

	public List<SoberTimeDisplayPreference> AllSoberTimeDisplayPreferences => Enum.GetValues(typeof(SoberTimeDisplayPreference)).Cast<SoberTimeDisplayPreference>().ToList();

	public SettingsViewModel(
		INotificationService notificationService,
		ISettingsService settingsService)
	{
		_notificationService = notificationService;
		_settingsService = settingsService;

		_notificationsEnabled = _settingsService.Get(PreferenceConstants.NotificationsEnabled, false);
		_notificationTime = _settingsService.Get(PreferenceConstants.NotificationTime, DateTime.MinValue);
		_soberDate = _settingsService.Get(PreferenceConstants.SoberDate, DateTime.Now);
		_soberTimeDisplayPreference = (SoberTimeDisplayPreference)_settingsService.Get(PreferenceConstants.SoberTimeDisplay, 0);
	}

	private async Task UpdateNotifications(bool enabled, DateTime time)
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

	partial void OnNotificationsEnabledChanged(bool value)
	{
		// Guard: never persist NotificationsEnabled = true on a platform where
		// notifications cannot fire. Forces the toggle back to false.
		if (value && !NotificationsSupported)
		{
			SetProperty(ref _notificationsEnabled, false, nameof(NotificationsEnabled));
			return;
		}

		_settingsService.Set(PreferenceConstants.NotificationsEnabled, value);
		_ = UpdateNotifications(value, NotificationTime);
	}

	partial void OnNotificationTimeChanged(DateTime value)
	{
		_settingsService.Set(PreferenceConstants.NotificationTime, value);
		_ = UpdateNotifications(NotificationsEnabled, value);
	}

	partial void OnSoberDateChanged(DateTime oldValue, DateTime newValue)
	{
		_settingsService.Set(PreferenceConstants.SoberDate, newValue);
		WeakReferenceMessenger.Default.Send(new SoberDateChangedMessage(newValue));
	}

	partial void OnSoberTimeDisplayPreferenceChanged(SoberTimeDisplayPreference oldValue, SoberTimeDisplayPreference newValue)
	{
		_settingsService.Set(PreferenceConstants.SoberTimeDisplay, (int)newValue);
		WeakReferenceMessenger.Default.Send(new SoberTimeDisplayPreferenceChangedMessage(newValue));
	}
}
