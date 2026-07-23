using Microsoft.UI.Xaml.Controls;

namespace DailyReflection.Views;

/// <summary>
/// Main page hosting TabBar region navigation.
/// All tab switching is driven by Uno.Extensions.Navigation regions —
/// no code-behind navigation logic required.
/// </summary>
public sealed partial class MainPage : Page
{
    public MainPage()
    {
        this.InitializeComponent();
    }
}
