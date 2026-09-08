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
		// Settings keeps the unset sober date as null for its DatePicker; here it is None.
		SoberDate = settings.SoberDate.WhereNotNull();
		DisplayPreference = settings.SoberTimeDisplayPreference;
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
