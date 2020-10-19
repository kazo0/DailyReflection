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
	public partial class DailyReflectionView : ContentPage
	{
		public DailyReflectionView()
		{
			InitializeComponent();
			var vm = Startup.ServiceProvider.GetService<DailyReflectionViewModel>();
			vm.Navigation = Navigation;
			BindingContext = vm;
		}

		protected override void OnAppearing()
		{
			base.OnAppearing();

			MainThread.InvokeOnMainThreadAsync(() => ((ViewModelBase)BindingContext).Init());
		}
	}
}