using DailyReflection.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace DailyReflection.Views
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class SobrietyTimeView : ContentPage
	{
		public SobrietyTimeView()
		{
			InitializeComponent();
			BindingContext = Startup.ServiceProvider.GetService<SobrietyTimeViewModel>();
		}
	}
}