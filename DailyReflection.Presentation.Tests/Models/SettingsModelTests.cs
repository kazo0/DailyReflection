using DailyReflection.Core.Constants;
using DailyReflection.Data.Models;
using DailyReflection.Presentation.Models;
using DailyReflection.Services.Notification;
using DailyReflection.Services.Settings;
using DailyReflection.Services.Theme;
using DailyReflection.Services.VersionTracking;
using Moq;
using NUnit.Framework;using Uno.Extensions.Reactive;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DailyReflection.Presentation.Tests.Models;

public class SettingsModelTests : ModelTestBase<SettingsModel>
{
	private Mock<INotificationService> _notificationService = null!;
	private Mock<ISettingsService> _settingsService = null!;
	private Mock<IVersionTrackingService> _versionTrackingService = null!;
	private Mock<IAppThemeService> _themeService = null!;

	private bool _notificationsEnabled = true;
	private AppThemePreference _themePreference = AppThemePreference.System;
	private DateTime _soberDate = new DateTime(2020, 12,31);
	private DateTime _notifTime = new DateTime(2020, 12, 31, 8, 30, 0);

	protected override void ResetDefaults()
	{
		_notificationsEnabled = true;
		_themePreference = AppThemePreference.System;
		_soberDate = new DateTime(2020, 12, 31);
		_notifTime = new DateTime(2020, 12, 31, 8, 30, 0);
	}

	protected override SettingsModel GetModel()
	{
		_notificationService = new Mock<INotificationService>();
		_notificationService.SetupGet(x => x.IsSupported).Returns(true);
		_settingsService = new Mock<ISettingsService>();
		_versionTrackingService = new Mock<IVersionTrackingService>();
		_versionTrackingService.SetupGet(x => x.CurrentVersion).Returns("4.0");
		_versionTrackingService.SetupGet(x => x.CurrentBuild).Returns("35");
		_themeService = new Mock<IAppThemeService>();

		_settingsService.Setup(x => x.Get(PreferenceConstants.NotificationsEnabled, It.IsAny<bool>()))
			.Returns(_notificationsEnabled);
		_settingsService.Setup(x => x.Get(PreferenceConstants.NotificationTime, It.IsAny<DateTime>()))
			.Returns(_notifTime);
		_settingsService.Setup(x => x.Get(PreferenceConstants.SoberDate, It.IsAny<DateTime>()))
			.Returns(_soberDate);
		_settingsService.Setup(x => x.Get(PreferenceConstants.AppThemePreference, It.IsAny<int>()))
			.Returns((int)_themePreference);

		return new SettingsModel(_notificationService.Object, _settingsService.Object, _versionTrackingService.Object, _themeService.Object);
	}

	[Test]
	public void MaxDate_Is_Now()
	{
		Assert.That(ModelUnderTest.MaxDate, Is.EqualTo(DateTime.Today));
	}

	[Test]
	public async Task Settings_Retrieved_On_Load()
	{
		Assert.That(await ModelUnderTest.NotificationsEnabled, Is.EqualTo(_notificationsEnabled));
		Assert.That(await ModelUnderTest.NotificationTime, Is.EqualTo(_notifTime));
		Assert.That(await ModelUnderTest.SoberDate, Is.EqualTo(_soberDate));

		_settingsService.Verify(x => x.Get(PreferenceConstants.NotificationsEnabled, It.IsAny<bool>()), Times.Once);
		_settingsService.Verify(x => x.Get(PreferenceConstants.NotificationTime, It.IsAny<DateTime>()), Times.Once);
		_settingsService.Verify(x => x.Get(PreferenceConstants.SoberDate, It.IsAny<DateTime>()), Times.Once);
	}

	[Test]
	public async Task Load_Does_Not_Persist_Or_Reschedule()
	{
		// Startup must be side-effect free (spec 002: re-scheduling on upgrade is
		// StartupMigrationRunner's job) — the ForEach initial replay is a load,
		// not a user edit.
		await ModelUnderTest.NotificationsEnabled;
		await Task.Delay(300);

		_settingsService.Verify(x => x.Set(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
		_notificationService.Verify(x => x.TryScheduleDailyNotification(It.IsAny<DateTime>(), It.IsAny<bool>()), Times.Never);
		_notificationService.Verify(x => x.CancelNotifications(), Times.Never);
		_themeService.Verify(x => x.ApplyTheme(It.IsAny<AppThemePreference>()), Times.Never);
	}

	[Test]
	public async Task Setting_NotificationsEnabled_False_Cancels_Notifications()
	{
		await ModelUnderTest.NotificationsEnabled.SetAsync(false, CancellationToken.None);

		await Eventually(() => _notificationService.Verify(x => x.CancelNotifications(), Times.Once));
	}

	[Test]
	public async Task Setting_NotificationsEnabled_False_Sets_Setting()
	{
		await ModelUnderTest.NotificationsEnabled.SetAsync(false, CancellationToken.None);

		await Eventually(() => _settingsService.Verify(x => x.Set(PreferenceConstants.NotificationsEnabled, false), Times.Once));
	}

	[Test]
	public async Task Setting_NotificationsEnabled_True_Schedules_Notification()
	{
		await ModelUnderTest.NotificationsEnabled.SetAsync(false, CancellationToken.None);
		await ModelUnderTest.NotificationsEnabled.SetAsync(true, CancellationToken.None);

		await Eventually(() => _notificationService.Verify(x => x.TryScheduleDailyNotification(_notifTime, true), Times.Once));
	}

	[Test]
	public async Task Setting_NotificationTime_With_Notifications_Enabled_Schedules_Notification()
	{
		await ModelUnderTest.NotificationTime.SetAsync(new DateTime(2020, 12, 31, 9, 0, 0), CancellationToken.None);

		await Eventually(() => _notificationService.Verify(x => x.TryScheduleDailyNotification(new DateTime(2020, 12, 31, 9, 0, 0), true), Times.Once));
	}

	[Test]
	public async Task Setting_NotificationTime_Without_Notifications_Enabled_Does_Not_Schedule_Notification()
	{
		_notificationsEnabled = false;
		var model = GetModel();

		await model.NotificationTime.SetAsync(new DateTime(2020, 12, 31, 9, 0, 0), CancellationToken.None);

		await Task.Delay(300);
		_notificationService.Verify(x => x.TryScheduleDailyNotification(It.IsAny<DateTime>(), It.IsAny<bool>()), Times.Never);
	}

	[Test]
	public async Task Setting_NotificationTime_Sets_Setting()
	{
		await ModelUnderTest.NotificationTime.SetAsync(new DateTime(2020, 12, 31, 9, 0, 0), CancellationToken.None);

		await Eventually(() => _settingsService.Verify(x => x.Set(PreferenceConstants.NotificationTime, new DateTime(2020, 12, 31, 9, 0, 0)), Times.Once));
	}

	[Test]
	public async Task Setting_SoberDate_Sets_Setting()
	{
		await ModelUnderTest.SoberDate.SetAsync(new DateTime(2020, 10, 20), CancellationToken.None);

		await Eventually(() => _settingsService.Verify(x => x.Set(PreferenceConstants.SoberDate, new DateTime(2020, 10, 20)), Times.Once));
	}

	[Test]
	public void NotificationsSupported_Reflects_NotificationService()
	{
		Assert.That(ModelUnderTest.NotificationsSupported, Is.True);

		_notificationService.SetupGet(x => x.IsSupported).Returns(false);
		var model = new SettingsModel(_notificationService.Object, _settingsService.Object, _versionTrackingService.Object, _themeService.Object);

		Assert.That(model.NotificationsSupported, Is.False);
	}

	[Test]
	public void AppVersion_Combines_Version_And_Build()
	{
		Assert.That(ModelUnderTest.AppVersion, Is.EqualTo("4.0 (35)"));
	}

	[Test]
	public async Task Setting_NotificationsEnabled_True_On_Unsupported_Platform_Reverts_To_False()
	{
		// Spec 002 §E — the toggle cannot stay on when IsSupported = false.
		// Store starts disabled so SetAsync(true) is a real change (states
		// dedupe same-value writes).
		_notificationsEnabled = false;
		var model = GetModel();
		_notificationService.SetupGet(x => x.IsSupported).Returns(false);

		await model.NotificationsEnabled.SetAsync(true, CancellationToken.None);

		await Eventually(async () =>
		{
			Assert.That(await model.NotificationsEnabled, Is.False,
				"NotificationsEnabled must revert to false when the platform is unsupported.");
		});
		_settingsService.Verify(x => x.Set(PreferenceConstants.NotificationsEnabled, true), Times.Never);
		_notificationService.Verify(x => x.TryScheduleDailyNotification(It.IsAny<DateTime>(), It.IsAny<bool>()), Times.Never);
	}

	[Test]
	public async Task AppThemePreference_Defaults_To_System()
	{
		// Fresh installs (and upgrades from the Xamarin app, which had no theme
		// setting) follow the OS theme.
		Assert.That(await ModelUnderTest.AppThemePreference, Is.EqualTo(AppThemePreference.System));
		_settingsService.Verify(x => x.Get(PreferenceConstants.AppThemePreference, It.IsAny<int>()), Times.Once);
	}

	[Test]
	public async Task AppThemePreference_Retrieved_On_Load()
	{
		_themePreference = AppThemePreference.Dark;
		var model = GetModel();

		Assert.That(await model.AppThemePreference, Is.EqualTo(AppThemePreference.Dark));
	}

	[Test]
	public async Task Setting_AppThemePreference_Sets_Setting_And_Applies_Theme()
	{
		await ModelUnderTest.AppThemePreference.SetAsync(AppThemePreference.Dark, CancellationToken.None);

		await Eventually(() =>
		{
			_settingsService.Verify(x => x.Set(PreferenceConstants.AppThemePreference, (int)AppThemePreference.Dark), Times.Once);
			_themeService.Verify(x => x.ApplyTheme(AppThemePreference.Dark), Times.Once);
		});
	}

	[Test]
	public async Task Setting_AppThemePreference_To_Stored_Value_Is_A_No_Op()
	{
		_themePreference = AppThemePreference.Light;
		var model = GetModel();

		await model.AppThemePreference.SetAsync(AppThemePreference.Light, CancellationToken.None);

		await Task.Delay(300);
		_settingsService.Verify(x => x.Set(PreferenceConstants.AppThemePreference, It.IsAny<int>()), Times.Never);
		_themeService.Verify(x => x.ApplyTheme(It.IsAny<AppThemePreference>()), Times.Never);
	}

	[Test]
	public void AllAppThemePreferences_Offers_System_Light_Dark_In_Order()
	{
		Assert.That(ModelUnderTest.AllAppThemePreferences,
			Is.EqualTo(new[] { AppThemePreference.System, AppThemePreference.Light, AppThemePreference.Dark }));
	}
}
