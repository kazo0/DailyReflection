using NodaTime;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace DailyReflection.ViewModels
{
	public class SobrietyTimeViewModel : ViewModelBase
	{
		public SobrietyTimeViewModel()
		{
			MessagingCenter.Subscribe<SettingsViewModel>(this, "SoberDate", (vm) => OnPropertyChanged(nameof(SoberPeriod)));
		}

		public Period SoberPeriod
		{
			get
			{
				var soberDate = Preferences.Get("SoberDate", DateTime.Now);
				var soberLocalDate = new LocalDate(soberDate.Year, soberDate.Month, soberDate.Day);
				return new LocalDate(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day) - soberLocalDate;
			}
		}

		public override async Task Init()
		{
		}

	}
}
