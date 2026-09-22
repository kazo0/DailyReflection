using DailyReflection.Presentation.Models;
using Microsoft.UI.Xaml.Input;

namespace DailyReflection.Views;

/// <summary>
/// Page for managing app settings. The MVUX navigator assigns the DataContext
/// (generated <see cref="SettingsViewModel"/>); every row writes the
/// model's states via TwoWay bindings (the TimePicker through
/// TimeOfDayConverter). The only code-behind opens the About Me page on the
/// developer's website.
/// </summary>
public sealed partial class SettingsPage : Page
{
	public SettingsPage()
	{
		this.InitializeComponent();
	}

	private async void AboutMe_Tapped(object sender, TappedRoutedEventArgs e)
	{
		await Windows.System.Launcher.LaunchUriAsync(new Uri("https://kazo0.dev/daily-reflection/"));
	}
}
