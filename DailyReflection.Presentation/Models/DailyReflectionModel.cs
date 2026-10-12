using CommunityToolkit.Mvvm.Messaging;
using DailyReflection.Core.Constants;
using DailyReflection.Core.Entities;
using DailyReflection.Services.DailyReflection;
using DailyReflection.Services.Settings;
using DailyReflection.Services.Share;
using System;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Reactive;
using Uno.Extensions.Reactive.Messaging;

namespace DailyReflection.Presentation.Models;

/// <summary>
/// MVUX model for the Reflection tab. The reflection feed is a projection of
/// the <see cref="Date"/> state combined with the reading preference observed
/// through MVUX messaging:
/// picking a date or flipping the secular preference reloads the feed
/// automatically (replacing the Init() / GetDailyReflection command pair).
/// </summary>
public partial record DailyReflectionModel
{
	private readonly IShareService _shareService;

	public DailyReflectionModel(
		IDailyReflectionService dailyReflectionService,
		IShareService shareService,
		ISettingsService settingsService,
		IMessenger messenger)
	{
		_shareService = shareService;
		Date = State.Value(this, () => DateTime.Today);
		// There is one reading preference, so its key stays constant when toggled.
		var preference = State.Value(this, () => new ReadingPreference(
			settingsService.Get(PreferenceConstants.SecularReadings, false)))
			.Observe(messenger, _ => PreferenceConstants.SecularReadings);
		DailyReflection = Feed
			.Combine(Date, preference)
			.SelectAsync(async (input, ct) => await dailyReflectionService.GetDailyReflection(input.Item1, input.Item2.IsSecular))
			// A null service result is the error state (no entry for the day);
			// Where publishes None for it, which the view renders as the error.
			.Where(reflection => reflection is not null);
		// One text size, so its key stays constant when changed (spec 013 §B).
		var textSize = State.Value(this, () => new ReadingTextSizeSelection(
			(ReadingTextSize)settingsService.Get(PreferenceConstants.ReadingTextSize, 0)))
			.Observe(messenger, _ => PreferenceConstants.ReadingTextSize);
		TextScale = textSize.Select(selection => selection.Size.ToScale());
	}

	/// <summary>
	/// The day being viewed. Set from the view when the user picks a date in the
	/// DatePickerFlyout; the reflection feed reloads off every change.
	/// </summary>
	public IState<DateTime> Date { get; }

	/// <summary>
	/// The reflection for <see cref="Date"/>. A null service result surfaces as
	/// None, which the view renders as the error state.
	/// </summary>
	public IFeed<Reflection> DailyReflection { get; }

	/// <summary>
	/// The factor the page multiplies its base font sizes and its column width
	/// by: the Text Size chosen in Settings (spec 013 §B). The OS text size is
	/// applied by the framework on top of it.
	/// </summary>
	public IFeed<double> TextScale { get; }

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
}
