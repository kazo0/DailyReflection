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
        private readonly INotificationService _notificationService;

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
            get =>  Preferences.Get(nameof(NotificationTime), DateTime.MinValue);
            set
            {
                Preferences.Set(nameof(NotificationTime), value);
                OnNotificationSettingsChanged();
            }
        }

        public DateTime SoberDate
        {
            get => Preferences.Get(nameof(SoberDate), DateTime.Now);
            set
            {
                Preferences.Set(nameof(SoberDate), value);
                OnSoberDateChanged();
            }
        }

        public DateTime MaxDate => DateTime.Now;

        private void OnSoberDateChanged()
		{
            MessagingCenter.Send(this, nameof(SoberDate));
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
