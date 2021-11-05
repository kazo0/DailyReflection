using DailyReflection.Presentation.Messages;
using DailyReflection.Presentation.ViewModels;
using Microsoft.Toolkit.Mvvm.Messaging;
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

		private void NotificationTimeTapped(object sender, EventArgs e)
		{
			this.TimePicker.Focus();
		}

		private void SoberTimeDisplayTapped(object sender, EventArgs e)
		{
			this.SoberTimeDisplayPicker.Focus();
		}

		private void SoberDateTapped(object sender, EventArgs e)
		{
			this.SoberDatePicker.Focus();
		}

		public void AppThemePreferenceTapped(object sender, EventArgs e)
		{
			this.AppThemePreferencePicker.Focus();
		}
	}
}