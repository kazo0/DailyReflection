using DailyReflection.Data.Models;
using NodaTime;
using NodaTime.Extensions;
using System;
using Uno.Extensions.Reactive;

namespace DailyReflection.Presentation.Models;

/// <summary>
/// MVUX model for the Sober Time tab. Pure projections of <see cref="SettingsModel"/>'s
/// states — when the user edits the sober date or display preference in Settings,
/// these feeds recompute automatically (replacing the WeakReferenceMessenger
/// SoberDateChangedMessage / SoberTimeDisplayPreferenceChangedMessage flow).
/// When no sober date is set the source state is None and every projection is
/// None as well; the view hides the date/period sections in that case.
/// </summary>
public partial record SobrietyTimeModel
{
	public SobrietyTimeModel(SettingsModel settings)
	{
		SoberDate = settings.SoberDate;
		DisplayPreference = settings.SoberTimeDisplayPreference;
		Years = settings.SoberDate.Select(date => GetSoberPeriod(date).Years);
		Months = settings.SoberDate.Select(date => GetSoberPeriod(date).Months);
		Days = settings.SoberDate.Select(date => GetSoberPeriod(date).Days);
		TotalDaysSober = settings.SoberDate.Select(GetTotalDaysSober);
	}

	public IFeed<DateTime> SoberDate { get; }

	public IFeed<SoberTimeDisplayPreference> DisplayPreference { get; }

	public IFeed<int> Years { get; }

	public IFeed<int> Months { get; }

	public IFeed<int> Days { get; }

	public IFeed<int> TotalDaysSober { get; }

	private static Period GetSoberPeriod(DateTime soberDate)
	{
		var soberLocalDate = new LocalDate(soberDate.Year, soberDate.Month, soberDate.Day);
		return new LocalDate(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day) - soberLocalDate;
	}

	private static int GetTotalDaysSober(DateTime soberDate)
	{
		return Period.Between(soberDate.ToLocalDateTime(), DateTime.Today.ToLocalDateTime(), PeriodUnits.Days).Days;
	}
}
