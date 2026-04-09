using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.Toolkit.UI;

namespace DailyReflection.Views;

/// <summary>
/// Main page hosting TabBar navigation.
/// Migrated from MAUI Shell to Uno Platform TabBar (Uno.Toolkit).
/// Based on Uno Platform Docs: NavigationShell.md
/// </summary>
public sealed partial class MainPage : Page
{
    private readonly Type[] _pageTypes = new[]
    {
        typeof(DailyReflectionPage),
        typeof(SobrietyTimePage),
        typeof(SettingsPage)
    };

    public MainPage()
    {
        this.InitializeComponent();
        this.Loaded += MainPage_Loaded;
    }

    private void MainPage_Loaded(object sender, RoutedEventArgs e)
    {
        // Navigate to first tab on load
        if (NavigationTabBar.SelectedIndex < 0)
        {
            NavigationTabBar.SelectedIndex = 0;
        }
        NavigateToTab(NavigationTabBar.SelectedIndex);
    }

    private void NavigationTabBar_SelectionChanged(TabBar sender, TabBarSelectionChangedEventArgs args)
    {
        var selectedIndex = NavigationTabBar.SelectedIndex;
        if (selectedIndex >= 0 && selectedIndex < _pageTypes.Length)
        {
            NavigateToTab(selectedIndex);
        }
    }

    private void NavigateToTab(int index)
    {
        if (index >= 0 && index < _pageTypes.Length)
        {
            ContentFrame.Navigate(_pageTypes[index]);
        }
    }
}
