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
			MessagingCenter.Subscribe<SettingsViewModel>(this, "SoberDate", (vm) => OnPropertyChanged(nameof(SobrietyTime)));
		}

        public TimeSpan SobrietyTime
		{
            get 
			{
				var soberDate = Preferences.Get("SoberDate", DateTime.Now);

				return DateTime.Now - soberDate;
			}
        }

        public override async Task Init()
		{
		}

	}
}
