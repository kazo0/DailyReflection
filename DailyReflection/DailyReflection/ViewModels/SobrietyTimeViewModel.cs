using DailyReflection.Constants;
using NodaTime;
using System;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace DailyReflection.ViewModels
{
	public class SobrietyTimeViewModel : ViewModelBase
	{
		private Period _soberPeriod; 

		public SobrietyTimeViewModel()
		{
			MessagingCenter.Subscribe<SettingsViewModel>(this, PreferenceConstants.SoberDate, (vm) => SoberPeriod = GetSoberPeriod());
			SoberPeriod = GetSoberPeriod();
		}


		public Period SoberPeriod
		{
			get => _soberPeriod;
			set => SetProperty(ref _soberPeriod, value);
		}

		private static Period GetSoberPeriod()
		{
			var soberDate = Preferences.Get(PreferenceConstants.SoberDate, DateTime.Now);
			var soberLocalDate = new LocalDate(soberDate.Year, soberDate.Month, soberDate.Day);
			return new LocalDate(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day) - soberLocalDate;
		}
	}
}
