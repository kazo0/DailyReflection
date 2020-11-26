﻿using DailyReflection.ViewModels;
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
	public partial class DailyReflectionView : ContentPage
	{
		public DailyReflectionView()
		{
			InitializeComponent();
			BindingContext = Startup.ServiceProvider.GetService<DailyReflectionViewModel>();
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