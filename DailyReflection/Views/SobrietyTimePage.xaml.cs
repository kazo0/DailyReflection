using DailyReflection.Presentation.ViewModels;
using System;

namespace DailyReflection.Views;

/// <summary>
/// Page displaying sobriety time information.
/// Lifecycle / VM activation are owned by <see cref="PageBase"/>.
/// </summary>
public sealed partial class SobrietyTimePage : PageBase
{
    public SobrietyTimeViewModel ViewModel { get; }
    protected override ViewModelBase ActiveViewModel => ViewModel;

    public SobrietyTimePage()
    {
        ViewModel = App.GetService<SobrietyTimeViewModel>();
        DataContext = ViewModel;
        this.InitializeComponent();
    }

    /// <summary>
    /// Format the sober date for display.
    /// </summary>
    public string FormatSoberDate(DateTime? date)
        => date?.ToString("MMM d, yyyy") ?? string.Empty;
}
