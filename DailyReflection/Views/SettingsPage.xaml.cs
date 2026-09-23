using DailyReflection.Presentation.Models;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace DailyReflection.Views;

/// <summary>
/// Page for managing app settings. The MVUX navigator assigns the DataContext
/// (generated <see cref="SettingsViewModel"/>); every row writes the
/// model's states via TwoWay bindings (the TimePicker through
/// TimeOfDayConverter). Code-behind forwards row taps to their switches and
/// opens the About Me page on the developer's website.
/// </summary>
public sealed partial class SettingsPage : Page
{
	public SettingsPage()
	{
		this.InitializeComponent();
	}

	private void ToggleRow_Tapped(object sender, TappedRoutedEventArgs e)
	{
		if (sender is not FrameworkElement { Tag: ToggleSwitch toggle } || !toggle.IsEnabled)
		{
			return;
		}

		// The switch handles its own input. Ignore taps bubbling from its template
		// so a direct switch tap cannot toggle the value a second time.
		for (var source = e.OriginalSource as DependencyObject; source is not null; source = VisualTreeHelper.GetParent(source))
		{
			if (source == toggle)
			{
				return;
			}
		}

		toggle.IsOn = !toggle.IsOn;
		e.Handled = true;
	}

	private async void AboutMe_Tapped(object sender, TappedRoutedEventArgs e)
	{
		await Windows.System.Launcher.LaunchUriAsync(new Uri("https://kazo0.dev/daily-reflection/"));
	}
}
