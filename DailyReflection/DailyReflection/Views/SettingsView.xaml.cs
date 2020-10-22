using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace DailyReflection.Views
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class SettingsView : ContentPage
	{
		public SettingsView()
		{
			InitializeComponent();
			BindingContext = Startup.ServiceProvider.GetService<DailyReflection.ViewModels.SettingsViewModel>();
		}

		private void ViewCell_Tapped(object sender, EventArgs e)
		{
			this.TimePicker.Focus();
		}

		private void Sober_ViewCell_Tapped(object sender, EventArgs e)
		{
			this.SoberDatePicker.Focus();
		}
	}
}