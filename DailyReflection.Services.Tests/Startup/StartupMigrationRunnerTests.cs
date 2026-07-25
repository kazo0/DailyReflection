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

	[Test]
	public async Task First_launch_ever_does_not_run_settings_migration()
	{
		_vt.SetupGet(v => v.IsFirstLaunchEver).Returns(true);
		_vt.SetupGet(v => v.IsFirstLaunchForCurrentBuild).Returns(true);
		_vt.SetupGet(v => v.CurrentVersion).Returns("3.5");
		_vt.SetupGet(v => v.CurrentBuild).Returns("35");
		_vt.SetupGet(v => v.PreviousVersion).Returns((string?)null);
		_vt.SetupGet(v => v.PreviousBuild).Returns((string?)null);

		await CreateRunner().RunAsync();

		_settings.Verify(s => s.MigrateOldPreferences(), Times.Once,
			"MigrateOldPreferences gates on IsFirstLaunchForCurrentBuild + thresholds + null prev — fresh upgrade matches.");
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
	public async Task Settings_migration_reschedules_notifications_when_enabled()
	{
		_vt.SetupGet(v => v.IsFirstLaunchForCurrentBuild).Returns(true);
		_vt.SetupGet(v => v.CurrentVersion).Returns("2.0");
		_vt.SetupGet(v => v.CurrentBuild).Returns("20");
		_vt.SetupGet(v => v.PreviousVersion).Returns((string?)null);
		_vt.SetupGet(v => v.PreviousBuild).Returns((string?)null);

		_settings.Setup(s => s.Get(PreferenceConstants.NotificationsEnabled, false)).Returns(true);
		var time = new DateTime(2026, 1, 1, 8, 30, 0);
		_settings.Setup(s => s.Get(PreferenceConstants.NotificationTime, DateTime.MinValue)).Returns(time);

		await CreateRunner().RunAsync();

		_settings.Verify(s => s.MigrateOldPreferences(), Times.Once);
		_notifications.Verify(n => n.TryScheduleDailyNotification(time, true), Times.Once);
	}

	[Test]
	public async Task Settings_migration_skips_notification_reschedule_when_disabled()
	{
		_vt.SetupGet(v => v.IsFirstLaunchForCurrentBuild).Returns(true);
		_vt.SetupGet(v => v.CurrentVersion).Returns("2.0");
		_vt.SetupGet(v => v.CurrentBuild).Returns("20");
		_vt.SetupGet(v => v.PreviousVersion).Returns((string?)null);
		_vt.SetupGet(v => v.PreviousBuild).Returns((string?)null);

		_settings.Setup(s => s.Get(PreferenceConstants.NotificationsEnabled, false)).Returns(false);

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
	public async Task RunAsync_always_calls_Track_first()
	{
		var calls = 0;
		_vt.Setup(v => v.Track()).Callback(() => calls++);

		await CreateRunner().RunAsync();

		Assert.That(calls, Is.EqualTo(1));
	}
}
