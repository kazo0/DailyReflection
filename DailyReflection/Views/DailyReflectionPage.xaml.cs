using DailyReflection.Presentation.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DailyReflection.Views;

/// <summary>
/// Page displaying the daily reflection content.
/// Migrated from MAUI DailyReflectionView.
/// </summary>
public sealed partial class DailyReflectionPage : Page
{
    public DailyReflectionViewModel ViewModel { get; }

    public DailyReflectionPage()
    {
        ViewModel = App.GetService<DailyReflectionViewModel>();
        this.InitializeComponent();
        ViewModel.IsActive = true;
        this.DataContext = ViewModel;

        // Load reflection when page is loaded
        this.Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Initial load of the daily reflection
        if (ViewModel.GetDailyReflectionCommand.CanExecute(null))
        {
            await ViewModel.GetDailyReflectionCommand.ExecuteAsync(null);
        }
    }

    /// <summary>
    /// Helper method for x:Bind to calculate content visibility.
    /// Content is visible when not loading and no error.
    /// </summary>
    public Visibility ShowContent(bool hasError, bool isLoading)
    {
        return (!hasError && !isLoading) ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void DatePickerFlyout_DatePicked(DatePickerFlyout sender, DatePickedEventArgs args)
    {
        ViewModel.Date = args.NewDate.DateTime;

        if (ViewModel.GetDailyReflectionCommand.CanExecute(null))
        {
            await ViewModel.GetDailyReflectionCommand.ExecuteAsync(null);
        }
    }
}
