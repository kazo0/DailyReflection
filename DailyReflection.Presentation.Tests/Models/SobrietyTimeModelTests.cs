using CommunityToolkit.Mvvm.Messaging;
using DailyReflection.Core.Constants;
using DailyReflection.Core.Entities;
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
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.Messaging;

namespace DailyReflection.Presentation.Tests.Models;

public class SobrietyTimeModelTests : ModelTestBase<SobrietyTimeModel>
{
	private Mock<ISettingsService> _settingsService = null!;
	private DateTime _soberDate = new DateTime(2020, 12, 31);
	private SoberTimeDisplayPreference _soberTimeDisplay = SoberTimeDisplayPreference.DaysOnly;
	private IMessenger _messenger = null!;
	private SettingsModel _settingsModel = null!;

	protected override void ResetDefaults()
	{
		_soberDate = new DateTime(2020, 12, 31);
		_soberTimeDisplay = SoberTimeDisplayPreference.DaysOnly;
	}

	protected override SobrietyTimeModel GetModel()
	{
		_settingsService = new Mock<ISettingsService>();
		_messenger = new WeakReferenceMessenger();
		var notificationService = new Mock<INotificationService>();
		var versionTrackingService = new Mock<IVersionTrackingService>();
		var clipboardService = new Mock<IClipboardService>();

		// Backed by the scenario fields so a model created after an edit reads the persisted value.
		_settingsService.Setup(s => s.Get(PreferenceConstants.SoberDate, It.IsAny<DateTime>()))
			.Returns(() => _soberDate);
		_settingsService.Setup(s => s.Set(PreferenceConstants.SoberDate, It.IsAny<DateTime>()))
			.Callback<string, DateTime>((_, value) => _soberDate = value);
		_settingsService.Setup(s => s.Get(PreferenceConstants.SoberTimeDisplay, It.IsAny<int>()))
			.Returns(() => (int)_soberTimeDisplay);
		_settingsService.Setup(s => s.Set(PreferenceConstants.SoberTimeDisplay, It.IsAny<int>()))
			.Callback<string, int>((_, value) => _soberTimeDisplay = (SoberTimeDisplayPreference)value);
		_settingsService.Setup(s => s.Get(PreferenceConstants.NotificationsEnabled, It.IsAny<bool>()))
			.Returns(false);
		_settingsService.Setup(s => s.Get(PreferenceConstants.NotificationTime, It.IsAny<DateTime>()))
			.Returns(DateTime.MinValue);

		_settingsModel = new SettingsModel(notificationService.Object, _settingsService.Object, versionTrackingService.Object, clipboardService.Object, _messenger);
		return new SobrietyTimeModel(_settingsService.Object, _messenger);
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
		Assert.That(await ModelUnderTest.SoberDate, Is.EqualTo(new DateTimeOffset(updated)));
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

	[Test]
	public async Task Picking_First_SoberDate_Shows_Period()
	{
		_soberDate = DateTime.MinValue;
		var model = GetModel();
		Assert.That((await model.SoberDate.Option(CancellationToken.None)).IsNone(), Is.True);

		var picked = DateTime.Today.AddDays(-7);
		await _settingsModel.SoberDate.SetAsync(new DateTimeOffset(picked), CancellationToken.None);

		await Eventually(async () => Assert.That(await model.TotalDaysSober, Is.EqualTo(7)));
	}

	[Test]
	public async Task SoberDate_Message_Before_First_Read_Uses_Updated_Date()
	{
		using var context = SourceContext.GetOrCreate(ModelUnderTest).AsCurrent();
		var sent = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		_messenger.Register<TaskCompletionSource, EntityMessage<SoberDateSelection>>(
			sent, static (recipient, _) => recipient.TrySetResult());

		var updated = DateTime.Today.AddDays(-10);
		await _settingsModel.SoberDate.SetAsync(new DateTimeOffset(updated), CancellationToken.None);
		await sent.Task.WaitAsync(TimeSpan.FromSeconds(5));

		await Eventually(async () => Assert.That(await ModelUnderTest.TotalDaysSober, Is.EqualTo(10)));
	}

	[Test]
	public async Task Model_Created_After_Edit_Loads_Persisted_Settings()
	{
		var updated = DateTime.Today.AddDays(-3);
		await _settingsModel.SoberDate.SetAsync(new DateTimeOffset(updated), CancellationToken.None);
		await _settingsModel.SoberTimeDisplayPreference.SetAsync(SoberTimeDisplayPreference.DaysMonthsYears, CancellationToken.None);
		await Eventually(() =>
		{
			Assert.That(_soberDate, Is.EqualTo(updated));
			Assert.That(_soberTimeDisplay, Is.EqualTo(SoberTimeDisplayPreference.DaysMonthsYears));
		});

		var model = new SobrietyTimeModel(_settingsService.Object, new WeakReferenceMessenger());

		Assert.That(await model.SoberDate, Is.EqualTo(new DateTimeOffset(updated)));
		Assert.That(await model.DisplayPreference, Is.EqualTo(SoberTimeDisplayPreference.DaysMonthsYears));
	}

	[Test]
	public async Task Unchanged_Settings_Send_No_Messages()
	{
		var received = 0;
		_messenger.Register<SobrietyTimeModelTests, EntityMessage<SoberDateSelection>>(
			this, (_, _) => Interlocked.Increment(ref received));
		_messenger.Register<SobrietyTimeModelTests, EntityMessage<SoberTimeDisplaySelection>>(
			this, (_, _) => Interlocked.Increment(ref received));

		await _settingsModel.SoberDate.SetAsync(new DateTimeOffset(_soberDate), CancellationToken.None);
		await _settingsModel.SoberTimeDisplayPreference.SetAsync(_soberTimeDisplay, CancellationToken.None);
		await ModelUnderTest.TotalDaysSober;
		await Task.Delay(100);

		Assert.That(received, Is.Zero);
	}
}
