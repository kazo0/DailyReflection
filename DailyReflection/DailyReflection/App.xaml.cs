using DailyReflection.Core.Constants;
using DailyReflection.Data.Databases;
using DailyReflection.Presentation.Messages;
using DailyReflection.Presentation.ViewModels;
using DailyReflection.Services;
using DailyReflection.Services.Notification;
using DailyReflection.Services.Settings;
using DailyReflection.Views;
using Microsoft.Toolkit.Mvvm.Messaging;
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

			MainPage = Startup.ServiceProvider.GetService<AppShell>();
		}

		private static async Task MigrateSettingsIfNeeded()
		{
			if (VersionTracking.IsFirstLaunchForCurrentBuild &&
				GetBuildVersion(VersionTracking.CurrentVersion) >= VersionConstants.NewSettingsVersion &&
				GetBuildVersion(VersionTracking.CurrentBuild) >= VersionConstants.NewSettingsBuild &&
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
					await notifService.TryScheduleDailyNotification(notifTime);
				}
			}
		}

		private static async Task RefreshDatabaseIfNeeded()
		{
			if (!VersionTracking.IsFirstLaunchEver &&
				VersionTracking.IsFirstLaunchForCurrentBuild &&
				VersionTracking.IsFirstLaunchForCurrentVersion &&
				GetBuildVersion(VersionTracking.CurrentVersion) >= VersionConstants.RefreshDatabaseVersion &&
				GetBuildVersion(VersionTracking.CurrentBuild) >= VersionConstants.RefreshDatabaseBuild &&
				GetBuildVersion(VersionTracking.PreviousBuild) < VersionConstants.RefreshDatabaseBuild &&
				GetBuildVersion(VersionTracking.PreviousVersion) < VersionConstants.RefreshDatabaseVersion)
			{
				var database = Startup.ServiceProvider.GetService<IDailyReflectionDatabase>();
				await database.RefreshDatabaseFile();
			}
		}

		private static double GetBuildVersion(string buildVersion)
		{
			if (double.TryParse(buildVersion, out var dblBuildVersion))
			{
				return dblBuildVersion;
			}

			return 0d;
		}

		protected override async void OnStart()
		{
			await MigrateSettingsIfNeeded();
			await RefreshDatabaseIfNeeded();
		}

		protected override void OnSleep()
		{
		}

		protected override void OnResume()
		{
		}

	}
}
