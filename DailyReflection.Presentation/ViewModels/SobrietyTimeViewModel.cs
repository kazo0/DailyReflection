using DailyReflection.Core.Constants;
using DailyReflection.Data.Models;
using DailyReflection.Presentation.Messages;
using DailyReflection.Services.Settings;
using Microsoft.Toolkit.Mvvm.Messaging;
using NodaTime;
using NodaTime.Extensions;
using System;
using Xamarin.Essentials;

namespace DailyReflection.Presentation.ViewModels
{
	public class SobrietyTimeViewModel : ViewModelBase, IRecipient<SoberDateChangedMessage>, IRecipient<SoberTimeDisplayPreferenceChangedMessage>
	{
		private Period _soberPeriod;
		private int _totalDaysSober;
		private DateTime? _soberDate;
		private SoberTimeDisplayPreference _displayPreference;
		private readonly ISettingsService _settingsService;

		public int TotalDaysSober
		{
			get => _totalDaysSober;
			set => SetProperty(ref _totalDaysSober, value);
		}
		public Period SoberPeriod
		{
			get => _soberPeriod;
			set => SetProperty(ref _soberPeriod, value);
		}

		public DateTime? SoberDate
		{
			get => _soberDate;
			set => SetProperty(ref _soberDate, value);
		}

		public SoberTimeDisplayPreference DisplayPreference
		{
			get => _displayPreference;
			set => SetProperty(ref _displayPreference, value);
		}

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
}
