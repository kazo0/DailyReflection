using DailyReflection.Core.Constants;
using DailyReflection.Services;
using DailyReflection.Services.Notification;
using DailyReflection.Services.Settings;
using DailyReflection.Views;
using System;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace DailyReflection
{
	public partial class App : Application
	{
		public App()
		{
			InitializeComponent();

			VersionTracking.Track();
			MigrateSettingsIfNeeded();

			MainPage = Startup.ServiceProvider.GetService<AppShell>();
		}

		private static void MigrateSettingsIfNeeded()
		{
			if (VersionTracking.IsFirstLaunchForCurrentBuild &&
				int.Parse(VersionTracking.CurrentVersion) >= VersionConstants.NewSettingsVersion &&
				int.Parse(VersionTracking.CurrentBuild) >= VersionConstants.NewSettingsBuild &&
				VersionTracking.PreviousBuild == null &&
				VersionTracking.PreviousVersion == null &&
				(Preferences.ContainsKey(PreferenceConstants.SoberDate) ||
				Preferences.ContainsKey(PreferenceConstants.NotificationsEnabled)))
			{
				var settingsService = Startup.ServiceProvider.GetService<ISettingsService>();
				settingsService.MigrateOldPreferences();

				if (settingsService.Get(PreferenceConstants.NotificationsEnabled, false))
				{
					var notifService = Startup.ServiceProvider.GetService<INotificationService>();
					var notifTime = settingsService.Get(PreferenceConstants.NotificationTime, DateTime.MinValue);
					notifService.ScheduleDailyNotification(notifTime);
				}
			}
		}

		protected override void OnStart()
		{
		}

		protected override void OnSleep()
		{
		}

		protected override void OnResume()
		{
		}
	}
}
