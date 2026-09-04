using DailyReflection.Data.Models;
using DailyReflection.Presentation.Models;
using DailyReflection.Services.DailyReflection;
using DailyReflection.Services.Share;
using Moq;
using NUnit.Framework;using Uno.Extensions.Reactive;
using Uno.Extensions.Reactive.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DailyReflection.Presentation.Tests.Models;

public class DailyReflectionModelTests : ModelTestBase<DailyReflectionModel>
{
	private Mock<IDailyReflectionService> _dailyReflectionService = null!;
	private Mock<IShareService> _shareService = null!;
	private Reflection _testReflection = null!;
	private List<Reflection> _reflections = null!;

	// Calendar order, as the service returns them: Jan 1, Feb 29, today, Dec 31
	// (today's entry is dropped when today is one of the fixed days).
	private const int Jan1Index = 0;
	private const int Feb29Index = 1;

	protected override DailyReflectionModel GetModel()
	{
		_dailyReflectionService = new Mock<IDailyReflectionService>();
		_shareService = new Mock<IShareService>();
		_testReflection = new Reflection
		{
			Id = 123,
			Month = DateTime.Today.Month,
			Day = DateTime.Today.Day,
			Reading = "Test Reading",
			Source = "Test Source",
			Thought = "Test Thought",
			Title = "Test",
		};

		_reflections = new List<Reflection>
		{
			new Reflection { Id = 1, Month = 1, Day = 1, Title = "Jan 1" },
			new Reflection { Id = 60, Month = 2, Day = 29, Title = "Feb 29" },
			_testReflection,
			new Reflection { Id = 366, Month = 12, Day = 31, Title = "Dec 31" },
		}
		.GroupBy(r => (r.Month, r.Day)).Select(g => g.First())
		.OrderBy(r => r.Month).ThenBy(r => r.Day)
		.ToList();

		_dailyReflectionService.Setup(x => x.GetAllReflections())
			.ReturnsAsync(_reflections);

		return new DailyReflectionModel(_dailyReflectionService.Object, _shareService.Object);
	}

	private int IndexOf(int month, int day) => _reflections.FindIndex(r => r.Month == month && r.Day == day);

	/// <summary>
	/// Feeds are cached per <see cref="SourceContext"/>: the bindable gives the
	/// model one context in the app, so every consumer of the Reflections feed
	/// shares one service call. Awaiting under the model's context here mirrors
	/// that; a bare await would open a fresh subscription (and a fresh call).
	/// </summary>
	private IDisposable UseModelContext() => SourceContext.GetOrCreate(ModelUnderTest).AsCurrent();

	[Test]
	public async Task Feed_Loads_Reflection_For_Today()
	{
		using var _ = UseModelContext();
		var reflection = await ModelUnderTest.DailyReflection;

		Assert.That(reflection, Is.Not.Null);
		Assert.That(reflection!.Id, Is.EqualTo(_testReflection.Id));
		_dailyReflectionService.Verify(x => x.GetAllReflections(), Times.Once);
	}

	[Test]
	public async Task Reflections_Loads_Every_Reading_Once()
	{
		using var _ = UseModelContext();
		var reflections = await ModelUnderTest.Reflections;
		await ModelUnderTest.DailyReflection;
		await ModelUnderTest.SelectedIndex;

		Assert.That(reflections.Select(r => r.Id), Is.EqualTo(_reflections.Select(r => r.Id)));
		_dailyReflectionService.Verify(x => x.GetAllReflections(), Times.Once);
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
	public async Task Missing_Reflection_Yields_None()
	{
		_dailyReflectionService.Reset();
		_dailyReflectionService.Setup(x => x.GetAllReflections())
			.ReturnsAsync(_reflections.Where(r => r != _testReflection).ToList());
		var model = new DailyReflectionModel(_dailyReflectionService.Object, _shareService.Object);

		var option = await model.DailyReflection.Option(CancellationToken.None);

		Assert.That(option.IsNone(), Is.True);
		Assert.That(await model.SelectedIndex, Is.EqualTo(-1));
	}

	[Test]
	public async Task Updating_Date_Reloads_Reflection()
	{
		await ModelUnderTest.DailyReflection;

		var picked = new DateTime(2020, 12, 31);
		await ModelUnderTest.Date.SetAsync(picked, CancellationToken.None);

		Assert.That(await ModelUnderTest.Date, Is.EqualTo(picked));
		await Eventually(async () =>
		{
			var reflection = await ModelUnderTest.DailyReflection;
			Assert.That(reflection?.Id, Is.EqualTo(366));
		});
	}

	[Test]
	public async Task SelectedIndex_Follows_Date()
	{
		Assert.That(await ModelUnderTest.SelectedIndex, Is.EqualTo(IndexOf(DateTime.Today.Month, DateTime.Today.Day)));

		await ModelUnderTest.Date.SetAsync(new DateTime(2020, 12, 31), CancellationToken.None);

		await Eventually(async () =>
		{
			Assert.That(await ModelUnderTest.SelectedIndex, Is.EqualTo(IndexOf(12, 31)));
		});
	}

	[Test]
	public async Task Setting_SelectedIndex_Updates_Date_To_That_Reading()
	{
		await ModelUnderTest.SelectedIndex;

		await ModelUnderTest.SelectedIndex.SetAsync(Jan1Index, CancellationToken.None);

		await Eventually(async () =>
		{
			Assert.That(await ModelUnderTest.Date, Is.EqualTo(new DateTime(DateTime.Today.Year, 1, 1)));
		});
		// The reflection shown/shared follows the paged-to date.
		await Eventually(async () =>
		{
			Assert.That((await ModelUnderTest.DailyReflection)?.Id, Is.EqualTo(1));
		});
	}

	[Test]
	public async Task Setting_SelectedIndex_To_Feb_29_Yields_A_Leap_Year_Date()
	{
		await ModelUnderTest.SelectedIndex;

		await ModelUnderTest.SelectedIndex.SetAsync(Feb29Index, CancellationToken.None);

		await Eventually(async () =>
		{
			var date = await ModelUnderTest.Date;
			Assert.That((date.Month, date.Day), Is.EqualTo((2, 29)));
			Assert.That(DateTime.IsLeapYear(date.Year), Is.True);
			Assert.That(date.Year, Is.LessThanOrEqualTo(DateTime.Today.Year));
		});
	}

	[Test]
	public async Task Setting_SelectedIndex_To_Current_Reading_Leaves_Date_Alone()
	{
		// The view echoes back the index it was given; that must not rewrite
		// Date (which would, e.g., drop a picked year).
		var picked = new DateTime(2020, 12, 31);
		await ModelUnderTest.Date.SetAsync(picked, CancellationToken.None);
		await Eventually(async () => Assert.That(await ModelUnderTest.SelectedIndex, Is.EqualTo(IndexOf(12, 31))));

		await ModelUnderTest.SelectedIndex.SetAsync(IndexOf(12, 31), CancellationToken.None);

		await Task.Delay(300);
		Assert.That(await ModelUnderTest.Date, Is.EqualTo(picked));
	}

	[Test]
	public async Task Setting_SelectedIndex_Out_Of_Range_Leaves_Date_Alone()
	{
		await ModelUnderTest.SelectedIndex;

		await ModelUnderTest.SelectedIndex.SetAsync(-1, CancellationToken.None);
		await ModelUnderTest.SelectedIndex.SetAsync(_reflections.Count, CancellationToken.None);

		await Task.Delay(300);
		Assert.That(await ModelUnderTest.Date, Is.EqualTo(DateTime.Today));
	}
}
