using DailyReflection.Presentation.ViewModels;
/* Unmerged change from project 'DailyReflection (net7.0-android)'
Before:
using System;
using Microsoft.Maui.Controls.Xaml;
using Microsoft.Maui.Controls;
using Microsoft.Maui;
After:
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;
using System;
*/


namespace DailyReflection.Views
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class DailyReflectionView : ContentPage
	{
		public DailyReflectionView(DailyReflectionViewModel viewModel)
		{
			InitializeComponent();
			BindingContext = viewModel;
		}

		protected override void OnAppearing()
		{
			base.OnAppearing();

			if (BindingContext is DailyReflectionViewModel vm)
			{
				MainThread.BeginInvokeOnMainThread(async () => await vm.Init());
			}
		}

		private void ToolbarItem_Clicked(object sender, EventArgs e)
		{
			DatePicker.Focus();
		}
	}
}