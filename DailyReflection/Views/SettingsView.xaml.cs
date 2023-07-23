using CommunityToolkit.Mvvm.Messaging;
using DailyReflection.Presentation.Messages;
using DailyReflection.Presentation.ViewModels;

namespace DailyReflection.Views
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class SettingsView : ContentPage
	{
		public SettingsView(SettingsViewModel viewModel)
		{
			InitializeComponent();
			BindingContext = viewModel;

			WeakReferenceMessenger.Default.Register<SettingsView, NotificationPermissionRequestMessage>(this, (r, m) =>
			{
				m.Reply(Dispatcher.DispatchAsync(() => r.DisplayAlert(
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
			this.TimePicker.IsVisible = true;
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