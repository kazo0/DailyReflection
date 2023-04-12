using DailyReflection.Presentation.Messages;
using DailyReflection.Presentation.ViewModels;
using Microsoft.Toolkit.Mvvm.Messaging;
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
	public partial class SettingsView : ContentPage
	{
		public SettingsView()
		{
			InitializeComponent();
			BindingContext = Startup.ServiceProvider.GetService<SettingsViewModel>();

			WeakReferenceMessenger.Default.Register<SettingsView, NotificationPermissionRequestMessage>(this, (r, m) =>
			{
				m.Reply(Device.InvokeOnMainThreadAsync(
					() => r.DisplayAlert(
						title: "Permission Required",
						message: "In order for notifications to work, permission must be granted in the System Settings. Press OK to be brought to your system's Notification Settings page.",
						accept: "OK",
						cancel: "Cancel")
					)
				);
			});
		}

		protected override void OnAppearing()
		{
			base.OnAppearing();
			StrongReferenceMessenger.Default.RegisterAll(this);
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