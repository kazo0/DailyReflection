using CommunityToolkit.Mvvm.Messaging;
using DailyReflection.Core.Constants;
using DailyReflection.Core.Entities;
using DailyReflection.Presentation.Models;
using DailyReflection.Services.Clipboard;
using DailyReflection.Services.DailyReflection;
using DailyReflection.Services.Notification;
using DailyReflection.Services.Settings;
using DailyReflection.Services.Share;
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

public class DailyReflectionModelTests : ModelTestBase<DailyReflectionModel>
{
	private Mock<IDailyReflectionService> _dailyReflectionService = null!;
	private Mock<IShareService> _shareService = null!;
	private Reflection _testReflection = null!;
	private Mock<ISettingsService> _settingsService = null!;
	private IMessenger _messenger = null!;

	protected override DailyReflectionModel GetModel()
	{
		_dailyReflectionService = new Mock<IDailyReflectionService>();
		_shareService = new Mock<IShareService>();
		_settingsService = new Mock<ISettingsService>();
		_messenger = new WeakReferenceMessenger();
		_testReflection = new Reflection
		{
			Id = 123,
			Reading = "Test Reading",
			Source = "Test Source",
			Thought = "Test Thought",
			Title = "Test",
		};

		_dailyReflectionService.Setup(x => x.GetDailyReflection(It.IsAny<DateTime?>(), It.IsAny<bool>()))
			.ReturnsAsync(_testReflection);

		return new DailyReflectionModel(_dailyReflectionService.Object, _shareService.Object, _settingsService.Object, _messenger);
	}

	private SettingsModel CreateSettings()
	{
		var notificationService = new Mock<INotificationService>();
		notificationService.SetupGet(x => x.IsSupported).Returns(true);
		var versionTrackingService = new Mock<IVersionTrackingService>();
		versionTrackingService.SetupGet(x => x.CurrentVersion).Returns("4.0");
		versionTrackingService.SetupGet(x => x.CurrentBuild).Returns("35");

		return new SettingsModel(
			notificationService.Object,
			_settingsService.Object,
			versionTrackingService.Object,
			new Mock<IClipboardService>().Object,
			_messenger);
	}

	[Test]
	public async Task Feed_Loads_Reflection_For_Today()
	{
		var reflection = await ModelUnderTest.DailyReflection;

		Assert.That(reflection, Is.Not.Null);
		Assert.That(reflection!.Id, Is.EqualTo(_testReflection.Id));
		_dailyReflectionService.Verify(x => x.GetDailyReflection(DateTime.Today, false), Times.Once);
	}

	[Test]
	public async Task Share_Calls_Share_Service()
	{
		await ModelUnderTest.DailyReflection;
		await ModelUnderTest.Share();

		_shareService.Verify(x => x.ShareText(
				$"Daily Reflection {DateTime.Today:MMM d}",
				_testReflection.ToString()),
			Times.Once);
	}

	[Test]
	public async Task Null_Reflection_Yields_None()
	{
		_dailyReflectionService.Reset();
		_dailyReflectionService.Setup(x => x.GetDailyReflection(It.IsAny<DateTime?>(), It.IsAny<bool>()))
			.ReturnsAsync(default(Reflection)!);

		var option = await ModelUnderTest.DailyReflection.Option(CancellationToken.None);

		Assert.That(option.IsNone(), Is.True);
	}

	[Test]
	public async Task Updating_Date_Reloads_Reflection()
	{
		await ModelUnderTest.DailyReflection;

		var picked = new DateTime(2020, 12, 31);
		await ModelUnderTest.Date.SetAsync(picked, CancellationToken.None);

		Assert.That(await ModelUnderTest.Date, Is.EqualTo(picked));
		await Eventually(() => _dailyReflectionService.Verify(x => x.GetDailyReflection(picked, false), Times.Once));
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task Toggling_Secular_Setting_Reloads_Selected_Date(bool initialSecular)
	{
		_messenger = new WeakReferenceMessenger();
		_settingsService.Setup(x => x.Get(PreferenceConstants.SecularReadings, false))
			.Returns(initialSecular);
		var settings = CreateSettings();
		var model = new DailyReflectionModel(_dailyReflectionService.Object, _shareService.Object, _settingsService.Object, _messenger);
		using var context = SourceContext.GetOrCreate(model).AsCurrent();
		var picked = new DateTime(2024, 2, 29);
		var traditional = _testReflection with { Title = "Traditional", IsSecular = false };
		var secular = _testReflection with { Title = "Secular", IsSecular = true };
		_dailyReflectionService.Setup(x => x.GetDailyReflection(picked, false)).ReturnsAsync(traditional);
		_dailyReflectionService.Setup(x => x.GetDailyReflection(picked, true)).ReturnsAsync(secular);
		await model.Date.SetAsync(picked, CancellationToken.None);
		Assert.That(await model.DailyReflection, Is.EqualTo(initialSecular ? secular : traditional));

		await settings.SecularReadings.SetAsync(!initialSecular, CancellationToken.None);

		await Eventually(async () => Assert.That(await model.DailyReflection,
			Is.EqualTo(initialSecular ? traditional : secular)));
		_settingsService.Verify(x => x.Set(PreferenceConstants.SecularReadings, !initialSecular), Times.Once);
		_dailyReflectionService.Verify(x => x.GetDailyReflection(picked, !initialSecular), Times.Once);
		Assert.That(await model.Date, Is.EqualTo(picked));
		await model.Share();
		_shareService.Verify(x => x.ShareText($"Daily Reflection {picked:MMM d}",
			(initialSecular ? traditional : secular).ToString()), Times.Once);
	}

	[Test]
	public async Task Preference_Message_Before_First_Read_Uses_Updated_Selection()
	{
		using var context = SourceContext.GetOrCreate(ModelUnderTest).AsCurrent();
		var secular = _testReflection with { IsSecular = true };
		_dailyReflectionService.Setup(x => x.GetDailyReflection(DateTime.Today, true)).ReturnsAsync(secular);
		var settings = CreateSettings();
		var sent = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		_messenger.Register<TaskCompletionSource, EntityMessage<ReadingPreference>>(
			sent, static (recipient, _) => recipient.TrySetResult());
		await settings.SecularReadings.SetAsync(true, CancellationToken.None);
		await sent.Task.WaitAsync(TimeSpan.FromSeconds(5));
		await Eventually(async () => Assert.That(await ModelUnderTest.DailyReflection, Is.EqualTo(secular)));
		_dailyReflectionService.Verify(x => x.GetDailyReflection(DateTime.Today, true), Times.Once);
	}

	[Test]
	public async Task Model_Created_After_Toggle_Loads_Persisted_Selection()
	{
		var stored = false;
		_settingsService.Setup(x => x.Get(PreferenceConstants.SecularReadings, false)).Returns(() => stored);
		_settingsService.Setup(x => x.Set(PreferenceConstants.SecularReadings, It.IsAny<bool>()))
			.Callback<string, bool>((_, value) => stored = value);
		var settings = CreateSettings();
		await settings.SecularReadings.SetAsync(true, CancellationToken.None);
		await Eventually(() => Assert.That(stored, Is.True));
		var secular = _testReflection with { IsSecular = true };
		_dailyReflectionService.Setup(x => x.GetDailyReflection(DateTime.Today, true)).ReturnsAsync(secular);

		var model = new DailyReflectionModel(_dailyReflectionService.Object, _shareService.Object,
			_settingsService.Object, new WeakReferenceMessenger());

		Assert.That(await model.DailyReflection, Is.EqualTo(secular));
		_dailyReflectionService.Verify(x => x.GetDailyReflection(DateTime.Today, true), Times.Once);
	}
}
