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
		private readonly ISettingsService _settingsService;

		public SobrietyTimeViewModel(ISettingsService settingsService)
		{
			_settingsService = settingsService;
			SoberPeriod = GetSoberPeriod();
		}

		public Period SoberPeriod
		{
			get => _soberPeriod;
			set => SetProperty(ref _soberPeriod, value);
		}

		private Period GetSoberPeriod()
		{
			var soberDate = _settingsService.Get(PreferenceConstants.SoberDate, DateTime.Now);
			var soberLocalDate = new LocalDate(soberDate.Year, soberDate.Month, soberDate.Day);
			return new LocalDate(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day) - soberLocalDate;
		}

		public void Receive(SoberDateChangedMessage message)
		{
			SoberPeriod = GetSoberPeriod();
		}
	}
}
