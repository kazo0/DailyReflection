using DailyReflection.Core.Constants;
using DailyReflection.Presentation.Messages;
using DailyReflection.Services.Settings;
using Microsoft.Toolkit.Mvvm.Messaging;
using NodaTime;
using System;
using Xamarin.Essentials;

namespace DailyReflection.Presentation.ViewModels
{
	public class SobrietyTimeViewModel : ViewModelBase, IRecipient<SoberDateChangedMessage>
	{
		private Period _soberPeriod;
		private DateTime? _soberDate;
		private readonly ISettingsService _settingsService;

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

		public SobrietyTimeViewModel(ISettingsService settingsService)
		{
			_settingsService = settingsService;
			SoberDate = GetSoberDate();
			SoberPeriod = GetSoberPeriod();
		}

		public void Receive(SoberDateChangedMessage message)
		{
			SoberDate = GetSoberDate();
			SoberPeriod = GetSoberPeriod();
		}

		private Period GetSoberPeriod()
		{
			var soberDate = SoberDate ?? DateTime.Now;
			var soberLocalDate = new LocalDate(soberDate.Year, soberDate.Month, soberDate.Day);
			return new LocalDate(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day) - soberLocalDate;
		}

		private DateTime? GetSoberDate()
		{
			var soberDate = _settingsService.Get(PreferenceConstants.SoberDate, DateTime.MinValue);
			return soberDate != DateTime.MinValue ? soberDate : null;
		}
	}
}
