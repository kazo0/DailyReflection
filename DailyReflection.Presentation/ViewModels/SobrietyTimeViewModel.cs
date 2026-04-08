using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using DailyReflection.Core.Constants;
using DailyReflection.Data.Models;
using DailyReflection.Presentation.Messages;
using DailyReflection.Services.Settings;
using NodaTime;
using NodaTime.Extensions;
using System;


namespace DailyReflection.Presentation.ViewModels;

public partial class SobrietyTimeViewModel : ViewModelBase, IRecipient<SoberDateChangedMessage>, IRecipient<SoberTimeDisplayPreferenceChangedMessage>
{
	[ObservableProperty]
	private Period _soberPeriod;

	[ObservableProperty]
	private int _totalDaysSober;

	[ObservableProperty]
	private DateTime? _soberDate;

	[ObservableProperty]
	private SoberTimeDisplayPreference _displayPreference;

	private readonly ISettingsService _settingsService;

	public SobrietyTimeViewModel(ISettingsService settingsService)
	{
		_settingsService = settingsService;
		SoberDate = GetSoberDate();
		SoberPeriod = GetSoberPeriod();
		DisplayPreference = GetDisplayPreference();
		TotalDaysSober = GetTotalDaysSober();
	}

	public void Receive(SoberDateChangedMessage message)
	{
		SoberDate = GetSoberDate();
		SoberPeriod = GetSoberPeriod();
		TotalDaysSober = GetTotalDaysSober();
	}

	public void Receive(SoberTimeDisplayPreferenceChangedMessage message)
	{
		DisplayPreference = GetDisplayPreference();
	}

	private int GetTotalDaysSober()
	{
		var soberDate = SoberDate ?? DateTime.Today;
		return Period.Between(soberDate.ToLocalDateTime(), DateTime.Today.ToLocalDateTime(), PeriodUnits.Days).Days;
	}

	private Period GetSoberPeriod()
	{
		var soberDate = SoberDate ?? DateTime.Today;
		var soberLocalDate = new LocalDate(soberDate.Year, soberDate.Month, soberDate.Day);
		return new LocalDate(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day) - soberLocalDate;
	}

	private DateTime? GetSoberDate()
	{
		var soberDate = _settingsService.Get(PreferenceConstants.SoberDate, DateTime.MinValue);
		return soberDate != DateTime.MinValue ? soberDate : default(DateTime?);
	}

	private SoberTimeDisplayPreference GetDisplayPreference()
	{
		return (SoberTimeDisplayPreference)_settingsService.Get(PreferenceConstants.SoberTimeDisplay, 0);
	}
}
