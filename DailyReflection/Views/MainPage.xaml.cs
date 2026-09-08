namespace DailyReflection.Views;

/// <summary>
/// Main page hosting the responsive shell: a bottom TabBar on narrow windows, a
/// vertical TabBar rail on wide ones. All tab switching is driven by
/// Uno.Extensions.Navigation regions declared in XAML — no code-behind logic.
/// </summary>
public sealed partial class MainPage : Page
{
	public MainPage()
	{
		this.InitializeComponent();
	}
}
