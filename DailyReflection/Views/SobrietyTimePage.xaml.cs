namespace DailyReflection.Views;

/// <summary>
/// Page displaying sobriety time information. The MVUX navigator assigns the
/// DataContext (generated SobrietyTimeViewModel); all values bind to flat
/// feeds projected from <c>SettingsModel</c>'s states.
/// </summary>
public sealed partial class SobrietyTimePage : Page
{
	public SobrietyTimePage()
	{
		this.InitializeComponent();
	}
}
