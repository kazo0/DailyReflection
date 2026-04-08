using DailyReflection.Presentation.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DailyReflection.Views;

/// <summary>
/// Page displaying sobriety time information.
/// Migrated from MAUI SobrietyTimeView.
/// </summary>
public sealed partial class SobrietyTimePage : Page
{
    public SobrietyTimeViewModel ViewModel { get; }

    public SobrietyTimePage()
    {
        ViewModel = App.GetService<SobrietyTimeViewModel>();
        this.InitializeComponent();
        ViewModel.IsActive = true;
        this.DataContext = ViewModel;

        Unloaded += OnUnloaded;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
    }

    /// <summary>
    /// Format the sober date for display.
    /// Replaces MAUI StringFormat binding.
    /// </summary>
    public string FormatSoberDate(DateTime? date)
    {
        return date?.ToString("MMM d, yyyy") ?? string.Empty;
    }
}
