using DailyReflection.Data.Models;
using DailyReflection.Services.DailyReflection;
using DailyReflection.Services.Share;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Reactive;

namespace DailyReflection.Presentation.Models;

/// <summary>
/// MVUX model for the Reflection tab. Every reading loads once into
/// <see cref="Reflections"/> (the FlipView's pages, ordered Jan 1 → Dec 31).
/// <see cref="Date"/> is the single source of truth for the day being viewed:
/// the reflection shown/shared (<see cref="DailyReflection"/>) and the FlipView
/// position (<see cref="SelectedIndex"/>) are both projections of it. Paging
/// the FlipView writes <see cref="SelectedIndex"/>, which maps back to
/// <see cref="Date"/>; picking a date moves the FlipView the other way round.
/// </summary>
public partial record DailyReflectionModel
{
	private readonly IShareService _shareService;
	private readonly IFeed<IImmutableList<Reflection>> _reflections;

	public DailyReflectionModel(IDailyReflectionService dailyReflectionService, IShareService shareService)
	{
		_shareService = shareService;
		Date = State.Value(this, () => DateTime.Today);

		Reflections = ListFeed.Async(async ct =>
			(IImmutableList<Reflection>)(await dailyReflectionService.GetAllReflections()).ToImmutableList());
		_reflections = Reflections.AsFeed();

		DailyReflection = Feed.Combine(_reflections, Date)
			.Select(x => FindReflection(x.Item1, x.Item2))
			// No entry for the day is the error state; Where publishes None for
			// it, which the view renders as the error.
			.Where(reflection => reflection is not null);

		SelectedIndex = State
			.FromFeed(this, Feed.Combine(_reflections, Date).Select(x => IndexOf(x.Item1, x.Item2)))
			.ForEach(OnSelectedIndexChanged);
	}

	/// <summary>
	/// The day being viewed. Set from the view when the user picks a date in the
	/// DatePickerFlyout, or indirectly by paging the FlipView (see
	/// <see cref="SelectedIndex"/>); every projection reloads off each change.
	/// </summary>
	public IState<DateTime> Date { get; }

	/// <summary>
	/// All readings in calendar order (Jan 1 → Dec 31, Feb 29 included). Loaded
	/// once; the FlipView's ItemsSource.
	/// </summary>
	public IListFeed<Reflection> Reflections { get; }

	/// <summary>
	/// The reflection for <see cref="Date"/>. No entry for the day surfaces as
	/// None, which the view renders as the error state.
	/// </summary>
	public IFeed<Reflection> DailyReflection { get; }

	/// <summary>
	/// Position of <see cref="Date"/>'s reading in <see cref="Reflections"/>
	/// (-1 when there is none). Follows <see cref="Date"/>; the view writes it
	/// when the user pages the FlipView, and that write is mapped back onto
	/// <see cref="Date"/>.
	/// </summary>
	public IState<int> SelectedIndex { get; }

	/// <summary>Share the current reflection (bound as the generated Share command).</summary>
	public async ValueTask Share(CancellationToken ct = default)
	{
		var reflection = await DailyReflection;
		if (reflection is null)
		{
			return;
		}

		var date = await Date;
		await _shareService.ShareText(
			title: $"Daily Reflection {date:MMM d}",
			body: reflection.ToString());
	}

	private async ValueTask OnSelectedIndexChanged(int index, CancellationToken ct)
	{
		if (index < 0)
		{
			return;
		}

		var reflections = await _reflections;
		if (reflections is null || index >= reflections.Count)
		{
			return;
		}

		// A callback carrying the index derived from Date (the state's own feed
		// replay, or the view echoing a value it was just given) is not a page
		// change and must not touch Date.
		var reflection = reflections[index];
		var date = await Date;
		if (date.Month == reflection.Month && date.Day == reflection.Day)
		{
			return;
		}

		await Date.SetAsync(ToDate(reflection), ct);
	}

	private static Reflection? FindReflection(IImmutableList<Reflection> reflections, DateTime date)
		=> reflections.FirstOrDefault(r => r.Month == date.Month && r.Day == date.Day);

	private static int IndexOf(IImmutableList<Reflection> reflections, DateTime date)
	{
		for (var i = 0; i < reflections.Count; i++)
		{
			if (reflections[i].Month == date.Month && reflections[i].Day == date.Day)
			{
				return i;
			}
		}

		return -1;
	}

	/// <summary>
	/// The FlipView pages by reading, not by calendar date, so a page maps to
	/// its date in the current year — except February 29, which only exists in
	/// leap years: it maps to the most recent one so the reading stays
	/// reachable by swiping (the date picker can't select it in other years).
	/// Only the month and day are ever displayed or shared.
	/// </summary>
	private static DateTime ToDate(Reflection reflection)
	{
		var year = DateTime.Today.Year;
		while (reflection.Day > DateTime.DaysInMonth(year, reflection.Month))
		{
			year--;
		}

		return new DateTime(year, reflection.Month, reflection.Day);
	}
}
