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

namespace DailyReflection.Presentation.Tests.Models;

public class DailyReflectionModelTests : ModelTestBase<DailyReflectionModel>
{
	private Mock<IDailyReflectionService> _dailyReflectionService = null!;
	private Mock<IShareService> _shareService = null!;
	private Reflection _testReflection = null!;

	protected override DailyReflectionModel GetModel()
	{
		_dailyReflectionService = new Mock<IDailyReflectionService>();
		_shareService = new Mock<IShareService>();
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

		return new DailyReflectionModel(_dailyReflectionService.Object, _shareService.Object, CreateSettings());
	}

	/// <summary>
	/// The model projects <see cref="SettingsModel.SecularReadings"/>, so the tests
	/// need a real SettingsModel over mocked services (its states are what the
	/// reflection feed combines with).
	/// </summary>
	private static SettingsModel CreateSettings()
	{
		var notificationService = new Mock<INotificationService>();
		notificationService.SetupGet(x => x.IsSupported).Returns(true);
		var versionTrackingService = new Mock<IVersionTrackingService>();
		versionTrackingService.SetupGet(x => x.CurrentVersion).Returns("4.0");
		versionTrackingService.SetupGet(x => x.CurrentBuild).Returns("35");

		return new SettingsModel(
			notificationService.Object,
			new Mock<ISettingsService>().Object,
			versionTrackingService.Object,
			new Mock<IClipboardService>().Object);
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

	[Test]
	public async Task Enabling_Secular_Readings_Reloads_Reflection_As_Secular()
	{
		var settings = CreateSettings();
		var model = new DailyReflectionModel(_dailyReflectionService.Object, _shareService.Object, settings);

		await model.DailyReflection;
		await settings.SecularReadings.SetAsync(true, CancellationToken.None);

		await Eventually(() => _dailyReflectionService.Verify(
			x => x.GetDailyReflection(DateTime.Today, true), Times.Once));
	}
}
