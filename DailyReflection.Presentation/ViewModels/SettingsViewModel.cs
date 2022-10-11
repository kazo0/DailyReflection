using DailyReflection.Core.Constants;
using DailyReflection.Data.Models;
using DailyReflection.Presentation.Messages;
using DailyReflection.Services;
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
				SetProperty(ref _soberDate, value);
				OnSoberDateChanged();
			}
		}

		private SoberTimeDisplayPreference _soberTimeDisplayPreference;

		public SoberTimeDisplayPreference SoberTimeDisplayPreference
		{
			get => _soberTimeDisplayPreference;
			set 
			{
				_settingsService.Set(PreferenceConstants.SoberTimeDisplay, (int)value);
				SetProperty(ref _soberTimeDisplayPreference, value);
				OnSoberTimeDisplayPreferenceChanged();

			}
		}

		public DateTime MaxDate => DateTime.Today;

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

		private void OnSoberDateChanged()
		{
			Messenger.Send(new SoberDateChangedMessage(SoberDate));
		}

		private void OnSoberTimeDisplayPreferenceChanged()
		{
			Messenger.Send(new SoberTimeDisplayPreferenceChangedMessage(SoberTimeDisplayPreference));
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
	}
}
