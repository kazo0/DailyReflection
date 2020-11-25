using DailyReflection.Constants;
using DailyReflection.Messages;
using DailyReflection.Services;
using Microsoft.Toolkit.Mvvm.Messaging;
using System;
using Xamarin.Essentials;

namespace DailyReflection.ViewModels
{
	public class SettingsViewModel : ViewModelBase
    {
        private readonly INotificationService _notificationService;

        private bool _notificationsEnabled;
        private DateTime _soberDate;
        private DateTime _notificationTime;

        public bool NotificationsEnabled
        {
            get => _notificationsEnabled;
            set
            {
                Preferences.Set(nameof(NotificationsEnabled), value);
                SetProperty(ref _notificationsEnabled, value);
                OnNotificationSettingsChanged();
            }
        }

        public DateTime NotificationTime
        {
            get => _notificationTime;
            set
            {
                Preferences.Set(nameof(NotificationTime), value);
                SetProperty(ref _notificationTime, value);
                OnNotificationSettingsChanged();
            }
        }

        public DateTime SoberDate
        {
            get => _soberDate;
            set
            {
                Preferences.Set(nameof(SoberDate), value);
                SetProperty(ref _soberDate, value);
                OnSoberDateChanged();
            }
        }

        public DateTime MaxDate => DateTime.Now;

        public SettingsViewModel(INotificationService notificationService)
		{
            _notificationService = notificationService;

            _notificationsEnabled = Preferences.Get(PreferenceConstants.NotificationsEnabled, false);
            _notificationTime = Preferences.Get(PreferenceConstants.NotificationTime, DateTime.MinValue);
            _soberDate = Preferences.Get(PreferenceConstants.SoberDate, DateTime.Now);
        }

        private void OnSoberDateChanged()
        {
            Messenger.Send(new SoberDateChangedMessage(SoberDate));
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
