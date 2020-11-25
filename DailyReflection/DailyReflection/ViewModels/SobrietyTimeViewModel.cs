using DailyReflection.Constants;
using DailyReflection.Messages;
using Microsoft.Toolkit.Mvvm.Messaging;
using NodaTime;
using System;
using Xamarin.Essentials;

namespace DailyReflection.ViewModels
{
	public class SobrietyTimeViewModel : ViewModelBase, IRecipient<SoberDateChangedMessage>
	{
		private Period _soberPeriod; 

		public SobrietyTimeViewModel()
		{
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

		public void Receive(SoberDateChangedMessage message)
		{
			SoberPeriod = GetSoberPeriod();
		}
	}
}
