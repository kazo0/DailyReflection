using DailyReflection.Presentation.Models;
using Microsoft.UI.Xaml.Input;

namespace DailyReflection.Views;

/// <summary>
/// Page for managing app settings. The MVUX navigator assigns the DataContext
/// (generated <see cref="BindableSettingsModel"/>); every row writes the
/// model's states via TwoWay bindings (the TimePicker through
/// TimeOfDayConverter). The only code-behind is the pickers' flyout scrim,
/// which Uno's DatePicker / TimePicker cannot take from XAML.
/// </summary>
public sealed partial class SettingsPage : Page
{
	public SettingsPage()
	{
		this.InitializeComponent();
	}

	private async void SupportMe_Tapped(object sender, TappedRoutedEventArgs e)
	{
		var dialog = new ContentDialog
		{
			XamlRoot = XamlRoot,
			Title = "Support Me!",
			Content = new TextBlock
			{
				Text = "I build and maintain this app in my free time. "
					+ "If it has been helpful to you, any support is greatly "
					+ "appreciated — but never expected. Thanks for being here!",
				TextWrapping = TextWrapping.Wrap,
			},
			PrimaryButtonText = "Buy me a coffee",
			CloseButtonText = "Cancel",
			DefaultButton = ContentDialogButton.Primary,
		};

		if (await dialog.ShowAsync() == ContentDialogResult.Primary)
		{
			await Windows.System.Launcher.LaunchUriAsync(new Uri("https://buymeacoffee.com/kazo0"));
		}
	}
}
