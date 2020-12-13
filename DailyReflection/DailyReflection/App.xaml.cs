using DailyReflection.Core.Constants;
using DailyReflection.Data.Databases;
using DailyReflection.Services;
using DailyReflection.Services.Notification;
using DailyReflection.Services.Settings;
using DailyReflection.Views;
using System;
using System.Threading.Tasks;
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
			RefreshDatabaseIfNeeded();

			MainPage = Startup.ServiceProvider.GetService<AppShell>();
		}

		private static void MigrateSettingsIfNeeded()
		{
			if (VersionTracking.IsFirstLaunchForCurrentBuild &&
				double.Parse(VersionTracking.CurrentVersion) >= VersionConstants.NewSettingsVersion &&
				double.Parse(VersionTracking.CurrentBuild) >= VersionConstants.NewSettingsBuild &&
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

		private static void RefreshDatabaseIfNeeded()
		{
			if (!VersionTracking.IsFirstLaunchEver &&
				VersionTracking.IsFirstLaunchForCurrentBuild &&
				VersionTracking.IsFirstLaunchForCurrentVersion &&
				double.Parse(VersionTracking.CurrentVersion) >= VersionConstants.RefreshDatabaseVersion &&
				double.Parse(VersionTracking.CurrentBuild) >= VersionConstants.RefreshDatabaseBuild &&
				double.Parse(VersionTracking.PreviousBuild) < VersionConstants.RefreshDatabaseBuild &&
				double.Parse(VersionTracking.PreviousVersion) <  VersionConstants.RefreshDatabaseVersion)
			{
				var database = Startup.ServiceProvider.GetService<IDailyReflectionDatabase>();
				Task.Run(async () => await database.RefreshDatabaseFile());
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
