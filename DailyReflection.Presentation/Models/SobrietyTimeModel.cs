using CommunityToolkit.Mvvm.Messaging;
using DailyReflection.Core.Constants;
using DailyReflection.Core.Entities;
using DailyReflection.Data.Models;
using DailyReflection.Services.Settings;
using NodaTime;
using NodaTime.Extensions;
using System;
using Uno.Extensions.Reactive;
using Uno.Extensions.Reactive.Messaging;

namespace DailyReflection.Presentation.Models;

/// <summary>
/// MVUX model for the Sober Time tab. The sober date and display preference are
/// seeded from <see cref="ISettingsService"/> and kept current through MVUX
/// messaging: <see cref="SettingsModel"/> persists each edit and sends an
/// <see cref="EntityMessage{T}"/> that <c>Observe</c> applies here, so the
/// period feeds recompute automatically (replacing the WeakReferenceMessenger
/// SoberDateChangedMessage / SoberTimeDisplayPreferenceChangedMessage flow).
/// When no sober date is set the date feed is None and every projection is
/// None as well; the view hides the date/period sections in that case.
/// </summary>
public partial record SobrietyTimeModel
{
	public SobrietyTimeModel(ISettingsService settingsService, IMessenger messenger)
	{
		// There is one sober date and one display preference, so each key stays
		// constant when edited. The seeds read the store lazily: a model first
		// subscribed after an edit still starts from the persisted value.
		var soberDate = State.Value(this, () => ReadSoberDate(settingsService))
			.Observe(messenger, _ => PreferenceConstants.SoberDate);
		var displayPreference = State.Value(this, () => new SoberTimeDisplaySelection(
			(SoberTimeDisplayPreference)settingsService.Get(PreferenceConstants.SoberTimeDisplay, 0)))
			.Observe(messenger, _ => PreferenceConstants.SoberTimeDisplay);

		// Where publishes None for an unset date, which the view hides.
		SoberDate = soberDate
			.Where(selection => selection.Date is not null)
			.Select(selection => new DateTimeOffset(selection.Date!.Value));
		DisplayPreference = displayPreference.Select(selection => selection.Preference);
		Years = SoberDate.Select(date => GetSoberPeriod(date).Years);
		Months = SoberDate.Select(date => GetSoberPeriod(date).Months);
		Days = SoberDate.Select(date => GetSoberPeriod(date).Days);
		TotalDaysSober = SoberDate.Select(GetTotalDaysSober);
	}

	public IFeed<DateTimeOffset> SoberDate { get; }

	public IFeed<SoberTimeDisplayPreference> DisplayPreference { get; }

	public IFeed<int> Years { get; }

	public IFeed<int> Months { get; }

	public IFeed<int> Days { get; }

	public IFeed<int> TotalDaysSober { get; }

	private static SoberDateSelection ReadSoberDate(ISettingsService settingsService)
	{
		// The store holds DateTime.MinValue until the user picks a sober date.
		var date = settingsService.Get(PreferenceConstants.SoberDate, DateTime.MinValue);
		return new SoberDateSelection(date == DateTime.MinValue ? null : date);
	}

	private static Period GetSoberPeriod(DateTimeOffset soberDate)
	{
		var soberLocalDate = new LocalDate(soberDate.Year, soberDate.Month, soberDate.Day);
		return new LocalDate(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day) - soberLocalDate;
	}

	private static int GetTotalDaysSober(DateTimeOffset soberDate)
	{
		return Period.Between(soberDate.Date.ToLocalDateTime(), DateTime.Today.ToLocalDateTime(), PeriodUnits.Days).Days;
	}
}
