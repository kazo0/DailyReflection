using DailyReflection.Core.Constants;
using DailyReflection.Data.Models;
using DailyReflection.Presentation.Models;
using DailyReflection.Services.Clipboard;
using DailyReflection.Services.Notification;
using DailyReflection.Services.Settings;
using DailyReflection.Services.VersionTracking;
using Moq;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Reactive;

namespace DailyReflection.Presentation.Tests.Models;

public class SobrietyTimeModelTests : ModelTestBase<SobrietyTimeModel>
{
	private Mock<ISettingsService> _settingsService = null!;
	private DateTime _soberDate = new DateTime(2020, 12, 31);
	private SoberTimeDisplayPreference _soberTimeDisplay = SoberTimeDisplayPreference.DaysOnly;
	private SettingsModel _settingsModel = null!;

	protected override void ResetDefaults()
	{
		_soberDate = new DateTime(2020, 12, 31);
		_soberTimeDisplay = SoberTimeDisplayPreference.DaysOnly;
	}

	protected override SobrietyTimeModel GetModel()
	{
		_settingsService = new Mock<ISettingsService>();
		var notificationService = new Mock<INotificationService>();
		var versionTrackingService = new Mock<IVersionTrackingService>();
		var clipboardService = new Mock<IClipboardService>();

		_settingsService.Setup(s => s.Get(PreferenceConstants.SoberDate, It.IsAny<DateTime>()))
			.Returns(_soberDate);
		_settingsService.Setup(s => s.Get(PreferenceConstants.SoberTimeDisplay, It.IsAny<int>()))
			.Returns((int)_soberTimeDisplay);
		_settingsService.Setup(s => s.Get(PreferenceConstants.NotificationsEnabled, It.IsAny<bool>()))
			.Returns(false);
		_settingsService.Setup(s => s.Get(PreferenceConstants.NotificationTime, It.IsAny<DateTime>()))
			.Returns(DateTime.MinValue);

		_settingsModel = new SettingsModel(notificationService.Object, _settingsService.Object, versionTrackingService.Object, clipboardService.Object);
		return new SobrietyTimeModel(_settingsModel);
	}

	[Test]
	public async Task SoberPeriod_Set_On_Load()
	{
		var soberLocalDate = new NodaTime.LocalDate(_soberDate.Year, _soberDate.Month, _soberDate.Day);
		var expected = new NodaTime.LocalDate(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day) - soberLocalDate;

		Assert.That(await ModelUnderTest.Years, Is.EqualTo(expected.Years));
		Assert.That(await ModelUnderTest.Months, Is.EqualTo(expected.Months));
		Assert.That(await ModelUnderTest.Days, Is.EqualTo(expected.Days));
	}

	[Test]
	public async Task SoberDate_Set_On_Load()
	{
		Assert.That(await ModelUnderTest.SoberDate, Is.EqualTo(new DateTimeOffset(_soberDate)));
	}

	[Test]
	public async Task DisplayPreference_Set_On_Load()
	{
		Assert.That(await ModelUnderTest.DisplayPreference, Is.EqualTo(_soberTimeDisplay));
	}

	[Test]
	public async Task Unset_SoberDate_Yields_None()
	{
		_soberDate = DateTime.MinValue;
		var model = GetModel();

		var option = await model.SoberDate.Option(CancellationToken.None);

		Assert.That(option.IsNone(), Is.True);
	}

	[Test]
	public async Task Changing_Settings_SoberDate_Recomputes_Period()
	{
		await ModelUnderTest.TotalDaysSober;

		var updated = DateTime.Today.AddDays(-30);
		await _settingsModel.SoberDate.SetAsync(new DateTimeOffset(updated), CancellationToken.None);

		await Eventually(async () =>
		{
			Assert.That(await ModelUnderTest.TotalDaysSober, Is.EqualTo(30));
		});
	}

	[Test]
	public async Task Changing_Settings_DisplayPreference_Updates_Feed()
	{
		Assert.That(await ModelUnderTest.DisplayPreference, Is.EqualTo(SoberTimeDisplayPreference.DaysOnly));

		await _settingsModel.SoberTimeDisplayPreference.SetAsync(SoberTimeDisplayPreference.DaysMonthsYears, CancellationToken.None);

		await Eventually(async () =>
		{
			Assert.That(await ModelUnderTest.DisplayPreference, Is.EqualTo(SoberTimeDisplayPreference.DaysMonthsYears));
		});
	}
}
