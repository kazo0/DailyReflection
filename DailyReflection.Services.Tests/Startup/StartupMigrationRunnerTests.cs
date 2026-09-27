using DailyReflection.Core.Constants;
using DailyReflection.Data.Databases;
using DailyReflection.Services.Notification;
using DailyReflection.Services.Settings;
using DailyReflection.Services.Startup;
using DailyReflection.Services.VersionTracking;
using Moq;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace DailyReflection.Services.Tests.Startup;

[TestFixture]
public class StartupMigrationRunnerTests
{
	private Mock<IVersionTrackingService> _vt = null!;
	private Mock<ISettingsService> _settings = null!;
	private Mock<INotificationService> _notifications = null!;
	private Mock<IDailyReflectionDatabase> _database = null!;

	[SetUp]
	public void Setup()
	{
		_vt = new Mock<IVersionTrackingService>();
		_settings = new Mock<ISettingsService>();
		_notifications = new Mock<INotificationService>();
		_database = new Mock<IDailyReflectionDatabase>();
	}

	private StartupMigrationRunner CreateRunner() =>
		new(_vt.Object, _settings.Object, _notifications.Object, _database.Object);

	// The versions the port actually ships with: NBGV's semantic versions (iOS uses the
	// same string for the build), and the packed Android versionCode.
	private static readonly (string Version, string Build)[] ShippedVersions =
	[
		("4.0.25", "4.0.25"),
		("4.0.25", "67108889"),
		("4.0.24-alpha-g79834442a6", "67108888"),
	];

	[TestCaseSource(nameof(ShippedVersions))]
	public async Task Legacy_settings_are_imported_on_the_first_launch_of_an_upgrade((string Version, string Build) shipped)
	{
		_vt.SetupGet(v => v.IsFirstLaunchEver).Returns(true);
		_vt.SetupGet(v => v.IsFirstLaunchForCurrentBuild).Returns(true);
		_vt.SetupGet(v => v.IsFirstLaunchForCurrentVersion).Returns(true);
		_vt.SetupGet(v => v.CurrentVersion).Returns(shipped.Version);
		_vt.SetupGet(v => v.CurrentBuild).Returns(shipped.Build);

		await CreateRunner().RunAsync();

		_settings.Verify(s => s.MigrateOldPreferences(), Times.Once);
		_settings.Verify(s => s.Set(PreferenceConstants.LegacySettingsImported, true), Times.Once);
	}

	[Test]
	public async Task Legacy_settings_are_not_imported_again_once_imported()
	{
		_settings.Setup(s => s.Get(PreferenceConstants.LegacySettingsImported, false)).Returns(true);

		await CreateRunner().RunAsync();

		_settings.Verify(s => s.MigrateOldPreferences(), Times.Never);
	}

	[Test]
	public void A_failed_import_is_retried_on_the_next_launch()
	{
		_settings.Setup(s => s.MigrateOldPreferences()).Throws(new InvalidOperationException("corrupt legacy store"));

		Assert.ThrowsAsync<InvalidOperationException>(() => CreateRunner().RunAsync());

		_settings.Verify(s => s.Set(PreferenceConstants.LegacySettingsImported, It.IsAny<bool>()), Times.Never);
	}

	[Test]
	public async Task First_launch_ever_does_not_refresh_database()
	{
		_vt.SetupGet(v => v.IsFirstLaunchEver).Returns(true);
		_vt.SetupGet(v => v.IsFirstLaunchForCurrentBuild).Returns(true);
		_vt.SetupGet(v => v.CurrentVersion).Returns("3.5");
		_vt.SetupGet(v => v.CurrentBuild).Returns("35");

		await CreateRunner().RunAsync();

		_database.Verify(d => d.RefreshDatabaseFile(), Times.Never,
			"RefreshDatabaseIfNeeded must skip the very first launch — there is nothing to refresh.");
	}

	[Test]
	public async Task Import_reschedules_notifications_and_may_prompt_when_enabled()
	{
		_notifications.SetupGet(n => n.IsSupported).Returns(true);
		_settings.Setup(s => s.Get(PreferenceConstants.NotificationsEnabled, false)).Returns(true);
		var time = new DateTime(2026, 1, 1, 8, 30, 0);
		_settings.Setup(s => s.Get(PreferenceConstants.NotificationTime, DateTime.MinValue)).Returns(time);

		await CreateRunner().RunAsync();

		_settings.Verify(s => s.MigrateOldPreferences(), Times.Once);
		_notifications.Verify(n => n.TryScheduleDailyNotification(time, true), Times.Once);
	}

	[Test]
	public async Task Every_launch_rearms_the_notification_without_prompting()
	{
		_notifications.SetupGet(n => n.IsSupported).Returns(true);
		_settings.Setup(s => s.Get(PreferenceConstants.LegacySettingsImported, false)).Returns(true);
		_settings.Setup(s => s.Get(PreferenceConstants.NotificationsEnabled, false)).Returns(true);
		var time = new DateTime(2026, 1, 1, 8, 30, 0);
		_settings.Setup(s => s.Get(PreferenceConstants.NotificationTime, DateTime.MinValue)).Returns(time);

		await CreateRunner().RunAsync();

		_notifications.Verify(n => n.TryScheduleDailyNotification(time, false), Times.Once);
	}

	[Test]
	public async Task Notifications_are_not_scheduled_when_disabled()
	{
		_notifications.SetupGet(n => n.IsSupported).Returns(true);
		_settings.Setup(s => s.Get(PreferenceConstants.NotificationsEnabled, false)).Returns(false);

		await CreateRunner().RunAsync();

		_notifications.Verify(n => n.TryScheduleDailyNotification(It.IsAny<DateTime>(), It.IsAny<bool>()), Times.Never);
	}

	[Test]
	public async Task Notifications_are_not_scheduled_where_unsupported()
	{
		_notifications.SetupGet(n => n.IsSupported).Returns(false);
		_settings.Setup(s => s.Get(PreferenceConstants.NotificationsEnabled, false)).Returns(true);

		await CreateRunner().RunAsync();

		_notifications.Verify(n => n.TryScheduleDailyNotification(It.IsAny<DateTime>(), It.IsAny<bool>()), Times.Never);
	}

	[Test]
	public async Task Database_refresh_runs_when_build_crosses_threshold()
	{
		_vt.SetupGet(v => v.IsFirstLaunchEver).Returns(false);
		_vt.SetupGet(v => v.IsFirstLaunchForCurrentBuild).Returns(true);
		_vt.SetupGet(v => v.IsFirstLaunchForCurrentVersion).Returns(true);
		_vt.SetupGet(v => v.CurrentVersion).Returns("3.2");
		_vt.SetupGet(v => v.CurrentBuild).Returns("32");
		_vt.SetupGet(v => v.PreviousVersion).Returns("3.1");
		_vt.SetupGet(v => v.PreviousBuild).Returns("31");

		await CreateRunner().RunAsync();

		_database.Verify(d => d.RefreshDatabaseFile(), Times.Once);
	}

	[Test]
	public async Task Database_refresh_skipped_when_below_threshold()
	{
		_vt.SetupGet(v => v.IsFirstLaunchEver).Returns(false);
		_vt.SetupGet(v => v.IsFirstLaunchForCurrentBuild).Returns(true);
		_vt.SetupGet(v => v.IsFirstLaunchForCurrentVersion).Returns(true);
		_vt.SetupGet(v => v.CurrentVersion).Returns("3.0");
		_vt.SetupGet(v => v.CurrentBuild).Returns("30");
		_vt.SetupGet(v => v.PreviousVersion).Returns("2.0");
		_vt.SetupGet(v => v.PreviousBuild).Returns("20");

		await CreateRunner().RunAsync();

		_database.Verify(d => d.RefreshDatabaseFile(), Times.Never);
	}

	[Test]
	public async Task Database_refresh_skipped_between_semantic_versions()
	{
		_vt.SetupGet(v => v.IsFirstLaunchEver).Returns(false);
		_vt.SetupGet(v => v.IsFirstLaunchForCurrentBuild).Returns(true);
		_vt.SetupGet(v => v.IsFirstLaunchForCurrentVersion).Returns(true);
		_vt.SetupGet(v => v.CurrentVersion).Returns("4.0.25");
		_vt.SetupGet(v => v.CurrentBuild).Returns("67108889");
		_vt.SetupGet(v => v.PreviousVersion).Returns("4.0.24");
		_vt.SetupGet(v => v.PreviousBuild).Returns("67108888");

		await CreateRunner().RunAsync();

		_database.Verify(d => d.RefreshDatabaseFile(), Times.Never);
	}

	[Test]
	[SetCulture("de-DE")]
	public async Task Database_refresh_reads_versions_independent_of_culture()
	{
		// de-DE reads "3.1" as 31 (a group separator), which would put the previous
		// version above the threshold and skip the refresh.
		_vt.SetupGet(v => v.IsFirstLaunchEver).Returns(false);
		_vt.SetupGet(v => v.IsFirstLaunchForCurrentBuild).Returns(true);
		_vt.SetupGet(v => v.IsFirstLaunchForCurrentVersion).Returns(true);
		_vt.SetupGet(v => v.CurrentVersion).Returns("3.2");
		_vt.SetupGet(v => v.CurrentBuild).Returns("32");
		_vt.SetupGet(v => v.PreviousVersion).Returns("3.1");
		_vt.SetupGet(v => v.PreviousBuild).Returns("31");

		await CreateRunner().RunAsync();

		_database.Verify(d => d.RefreshDatabaseFile(), Times.Once);
	}

	[Test]
	public async Task RunAsync_always_calls_Track_first()
	{
		var calls = 0;
		_vt.Setup(v => v.Track()).Callback(() => calls++);

		await CreateRunner().RunAsync();

		Assert.That(calls, Is.EqualTo(1));
	}
}
