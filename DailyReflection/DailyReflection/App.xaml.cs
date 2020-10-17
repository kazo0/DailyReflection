using DailyReflection.Views;
using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace DailyReflection
{
	public partial class App : Application
	{
		public App()
		{
			InitializeComponent();
			Startup.Init();
			MainPage = new NavigationPage(new DailyReflectionView());
		}

		protected override void OnStart()
		{
		}

		protected override void OnSleep()
		{
		}

		protected override void OnResume()
		{
		}
	}
}
