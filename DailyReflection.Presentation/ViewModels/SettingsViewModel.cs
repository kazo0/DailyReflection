using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using DailyReflection.Core.Constants;
using DailyReflection.Data.Models;
using DailyReflection.Presentation.Messages;
using DailyReflection.Services.Notification;
using DailyReflection.Services.Settings;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

	public DateTimeOffset MaxDate => DateTimeOffset.Now.Date;

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

	protected override void OnPropertyChanged(PropertyChangedEventArgs e)
	{
		base.OnPropertyChanged(e);

		if (e.PropertyName == nameof(NotificationsEnabled))
		{
			Task.Run(UpdateNotifications);
		}
		else if (e.PropertyName == nameof(NotificationTime))
		{
			Task.Run(UpdateNotifications);
		}
	}

	private async Task UpdateNotifications()
	{
		if (NotificationsEnabled)
		{
			await _notificationService.TryScheduleDailyNotification(NotificationTime);
		}
		else
		{
			_notificationService.CancelNotifications();
		}
	}

	partial void OnNotificationsEnabledChanged(bool value)
	{
		Task.Run(UpdateNotifications);
	}

	partial void OnNotificationTimeChanged(DateTime value)
	{
		Task.Run(UpdateNotifications);
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
