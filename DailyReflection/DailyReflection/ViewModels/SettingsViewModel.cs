using DailyReflection.Constants;
using DailyReflection.Services;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;

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

        public SettingsViewModel()
		{
            _notificationService = DependencyService.Get<INotificationService>();
            _notificationsEnabled = Preferences.Get(PreferenceConstants.NotificationsEnabled, false);
            _notificationTime = Preferences.Get(PreferenceConstants.NotificationTime, DateTime.MinValue);
            _soberDate = Preferences.Get(PreferenceConstants.SoberDate, DateTime.Now);
        }

        private void OnSoberDateChanged()
        {
            MessagingCenter.Send(this, PreferenceConstants.SoberDate);
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
