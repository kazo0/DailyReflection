using DailyReflection.Services;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace DailyReflection.ViewModels
{
	public class SettingsViewModel : ViewModelBase
	{
        private INotificationService _notificationService;

		public SettingsViewModel()
		{
            _notificationService = DependencyService.Get<INotificationService>();
		}

		public async override Task Init()
		{

		}

		public bool NotificationsEnabled
        {
            get => Preferences.Get(nameof(NotificationsEnabled), false);
            set
            {
                Preferences.Set(nameof(NotificationsEnabled), value);
                OnNotificationSettingsChanged();
            }
        }

        public DateTime NotificationTime
        {
            get => Preferences.Get(nameof(NotificationTime), DateTime.MinValue);
            set
            {
                Preferences.Set(nameof(NotificationTime), value);
                OnNotificationSettingsChanged();
            }
        }

        public bool NotificationSound
        {
            get => Preferences.Get(nameof(NotificationSound), false);
            set
            {
                Preferences.Set(nameof(NotificationSound), value);
                OnNotificationSettingsChanged();
            }
        }

        public bool NotificationVibrate
        {
            get => Preferences.Get(nameof(NotificationVibrate), false);
            set
            {
                Preferences.Set(nameof(NotificationVibrate), value);
                OnNotificationSettingsChanged();
            }
        }


        private void OnNotificationSettingsChanged()
		{
            if (NotificationsEnabled)
            {
                _notificationService.ScheduleDailyNotification();
            } 
            else
			{
                _notificationService.CancelNotifications();
			}
		}
    }
}
