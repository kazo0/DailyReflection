using DailyReflection.Core.Constants;
using DailyReflection.Data.Models;
using DailyReflection.Presentation.Entities;
using DailyReflection.Presentation.Messages;
using DailyReflection.Services.Notification;
using DailyReflection.Services.Settings;
using Microsoft.Toolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using Xamarin.Essentials;

namespace DailyReflection.Presentation.ViewModels
{
	public class SettingsViewModel : ViewModelBase
	{
		private readonly INotificationService _notificationService;
		private readonly ISettingsService _settingsService;
		private bool _notificationsEnabled;
		private DateTime _soberDate;
		private DateTime _notificationTime;

		public bool NotificationsEnabled
		{
			get => _notificationsEnabled;
			set
			{
				_settingsService.Set(PreferenceConstants.NotificationsEnabled, value);
				SetProperty(ref _notificationsEnabled, value);
				OnNotificationSettingsChanged();
			}
		}

		public DateTime NotificationTime
		{
			get => _notificationTime;
			set
			{
				_settingsService.Set(PreferenceConstants.NotificationTime, value);
				SetProperty(ref _notificationTime, value);
				OnNotificationSettingsChanged();
			}
		}

		public DateTime SoberDate
		{
			get => _soberDate;
			set
			{
				_settingsService.Set(PreferenceConstants.SoberDate, value);
				SetProperty(ref _soberDate, value, broadcast: true);
			}
		}

		private SoberTimeDisplayPreference _soberTimeDisplayPreference;

		public SoberTimeDisplayPreference SoberTimeDisplayPreference
		{
			get => _soberTimeDisplayPreference;
			set
			{
				_settingsService.Set(PreferenceConstants.SoberTimeDisplay, (int)value);
				SetProperty(ref _soberTimeDisplayPreference, value, broadcast: true);
			}
		}

		private AppThemePreference _appThemePreference;
		public AppThemePreference AppThemePreference
		{
			get => _appThemePreference;
			set
			{
				_settingsService.Set(PreferenceConstants.AppThemePreference, (int)value);
				SetProperty(ref _appThemePreference, value);
				OnAppThemePreferenceChanged();
			}
		}

		public DateTime MaxDate => DateTime.Today;

		public List<SoberTimeDisplayPreference> AllSoberTimeDisplayPreferences => Enum.GetValues(typeof(SoberTimeDisplayPreference)).Cast<SoberTimeDisplayPreference>().ToList();
		public List<AppThemePreference> AllAppThemePreferences => Enum.GetValues(typeof(AppThemePreference)).Cast<AppThemePreference>().ToList();

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
			_appThemePreference = (AppThemePreference)_settingsService.Get(PreferenceConstants.AppThemePreference, 0);
		}

		private void OnNotificationSettingsChanged()
		{
			if (NotificationsEnabled)
			{
				_notificationService.ScheduleDailyNotification(NotificationTime);
			}
			else
			{
				_notificationService.CancelNotifications();
			}
		}

		private void OnAppThemePreferenceChanged()
		{
			Messenger.Send(new AppThemePreferenceChangedMessage(AppThemePreference));
		}
	}
}
