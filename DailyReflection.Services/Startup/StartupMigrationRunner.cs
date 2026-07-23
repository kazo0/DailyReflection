using DailyReflection.Core.Constants;
using DailyReflection.Data.Databases;
using DailyReflection.Services.Notification;
using DailyReflection.Services.Settings;
using DailyReflection.Services.VersionTracking;
using System;
using System.Threading.Tasks;

namespace DailyReflection.Services.Startup;

/// <summary>
/// Ports the version-gated startup migrations from the Xamarin original
/// (<c>App.OnStart → MigrateSettingsIfNeeded / RefreshDatabaseIfNeeded</c>).
/// Resolved once during app startup; <see cref="RunAsync"/> drives the version tracker
/// and triggers each migration if its threshold has been crossed.
/// </summary>
public class StartupMigrationRunner
{
	private readonly IVersionTrackingService _versionTracking;
	private readonly ISettingsService _settings;
	private readonly INotificationService _notifications;
	private readonly IDailyReflectionDatabase _database;

	public StartupMigrationRunner(
		IVersionTrackingService versionTracking,
		ISettingsService settings,
		INotificationService notifications,
		IDailyReflectionDatabase database)
	{
		_versionTracking = versionTracking;
		_settings = settings;
		_notifications = notifications;
		_database = database;
	}

	public async Task RunAsync()
	{
		_versionTracking.Track();
		await MigrateSettingsIfNeeded();
		await RefreshDatabaseIfNeeded();
	}

	private async Task MigrateSettingsIfNeeded()
	{
		// First launch of the build that introduced the new settings layout, with no
		// previous build/version recorded — i.e. an upgrade from a pre-tracking install.
		if (!(_versionTracking.IsFirstLaunchForCurrentBuild
			&& ParseBuild(_versionTracking.CurrentVersion) >= VersionConstants.NewSettingsVersion
			&& ParseBuild(_versionTracking.CurrentBuild) >= VersionConstants.NewSettingsBuild
			&& _versionTracking.PreviousBuild == null
			&& _versionTracking.PreviousVersion == null))
		{
			return;
		}

		_settings.MigrateOldPreferences();

		if (_settings.Get(PreferenceConstants.NotificationsEnabled, false))
		{
			var notifTime = _settings.Get(PreferenceConstants.NotificationTime, DateTime.MinValue);
			await _notifications.TryScheduleDailyNotification(notifTime);
		}
	}

	private async Task RefreshDatabaseIfNeeded()
	{
		if (!(!_versionTracking.IsFirstLaunchEver
			&& _versionTracking.IsFirstLaunchForCurrentBuild
			&& _versionTracking.IsFirstLaunchForCurrentVersion
			&& ParseBuild(_versionTracking.CurrentVersion) >= VersionConstants.RefreshDatabaseVersion
			&& ParseBuild(_versionTracking.CurrentBuild) >= VersionConstants.RefreshDatabaseBuild
			&& ParseBuild(_versionTracking.PreviousBuild) < VersionConstants.RefreshDatabaseBuild
			&& ParseBuild(_versionTracking.PreviousVersion) < VersionConstants.RefreshDatabaseVersion))
		{
			return;
		}

		await _database.RefreshDatabaseFile();
	}

	private static double ParseBuild(string? value)
		=> double.TryParse(value, out var d) ? d : 0d;
}
