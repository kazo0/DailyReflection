using DailyReflection.Presentation.ViewModels;
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
			this.DatePicker.Open();
		}
	}
}