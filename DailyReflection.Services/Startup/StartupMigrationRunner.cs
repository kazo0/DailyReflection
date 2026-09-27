using DailyReflection.Core.Constants;
using DailyReflection.Data.Databases;
using DailyReflection.Services.Notification;
using DailyReflection.Services.Settings;
using DailyReflection.Services.VersionTracking;
using System;
using System.Globalization;
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
		var imported = MigrateSettingsIfNeeded();
		await RefreshDatabaseIfNeeded();
		await RestoreDailyNotification(shouldRequestPermission: imported);
	}

	private bool MigrateSettingsIfNeeded()
	{
		// The Xamarin app gated this on the first launch of a build >= 2.0/20 with no
		// version tracked yet. That gate can't carry over: NBGV's "4.0.N" versions are
		// not numbers, and the version tracker's keys are new, so every upgrade from the
		// Xamarin app looks untracked here anyway. Import once per install instead,
		// recorded by its own flag — a no-op on fresh installs, where the legacy stores
		// are empty. The flag is only set once the import succeeds, so a failed import
		// is retried on the next launch.
		if (_settings.Get(PreferenceConstants.LegacySettingsImported, false))
		{
			return false;
		}

		_settings.MigrateOldPreferences();
		_settings.Set(PreferenceConstants.LegacySettingsImported, true);
		return true;
	}

	private async Task RestoreDailyNotification(bool shouldRequestPermission)
	{
		// Re-arm the daily reminder on every launch: the Android alarm does not survive a
		// force-stop, its receiver skips re-arming while notification permission is
		// revoked, and the Windows desktop timer only lives as long as the process. Each
		// platform replaces its existing request, so repeating this is harmless. Only the
		// launch that imported the legacy settings may prompt for permission, as the
		// Xamarin migration did; ordinary launches never prompt.
		if (!_notifications.IsSupported || !_settings.Get(PreferenceConstants.NotificationsEnabled, false))
		{
			return;
		}

		var notifTime = _settings.Get(PreferenceConstants.NotificationTime, DateTime.MinValue);
		await _notifications.TryScheduleDailyNotification(notifTime, shouldRequestPermission);
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

	// Reads "3.2", "32", an Android versionCode, or an NBGV version ("4.0.24",
	// "4.0.24-alpha-g1a2b3c") as major.minor, independent of the device's culture.
	private static double ParseBuild(string? value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return 0d;
		}

		var parts = value.Split('-', '+')[0].Split('.');
		var majorMinor = parts.Length > 1 ? $"{parts[0]}.{parts[1]}" : parts[0];
		return double.TryParse(majorMinor, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : 0d;
	}
}
