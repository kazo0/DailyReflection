using DailyReflection.Presentation.ViewModels;
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
			BindingContext = Startup.ServiceProvider.GetService<SettingsViewModel>();
		}

		protected override void OnAppearing()
		{
			base.OnAppearing();

			if (BindingContext is ViewModelBase vm)
			{
				vm.IsActive = true;
			}
		}

		protected override void OnDisappearing()
		{
			base.OnDisappearing();
			if (BindingContext is ViewModelBase vm)
			{
				vm.IsActive = false;
			}
		}

		private void Notification_Time_Tapped(object sender, EventArgs e)
		{
			this.TimePicker.Focus();
		}

		private void SoberTimeDisplay_Tapped(object sender, EventArgs e)
		{
			this.SoberTimeDisplayPicker.Focus();
		}

		private void Sober_Date_Tapped(object sender, EventArgs e)
		{
			this.SoberDatePicker.Focus();
		}
	}
}