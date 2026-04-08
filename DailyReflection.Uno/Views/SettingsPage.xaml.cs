using DailyReflection.Core.Constants;
using DailyReflection.Presentation.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace DailyReflection.Views;

/// <summary>
/// Page for managing app settings.
/// Migrated from MAUI SettingsView.
/// </summary>
public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel { get; }
    
    /// <summary>
    /// App version for display.
    /// </summary>
    public string AppVersion => VersionConstants.VersionNumber;

    public SettingsPage()
    {
        ViewModel = App.GetService<SettingsViewModel>();
        this.InitializeComponent();
        ViewModel.IsActive = true;
        this.DataContext = ViewModel;
    }

    /// <summary>
    /// Format notification time for display.
    /// Replaces MAUI StringFormat binding.
    /// </summary>
    public string FormatNotificationTime(DateTime time)
    {
        return time.ToString("h:mm tt");
    }

    /// <summary>
    /// Format sober date for display.
    /// Replaces MAUI StringFormat binding.
    /// </summary>
    public string FormatSoberDate(DateTime date)
    {
        return date.ToString("MMM d, yyyy");
    }

    /// <summary>
    /// Handle tap on notification time cell - shows time picker.
    /// </summary>
    private void NotificationTime_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (!ViewModel.NotificationsEnabled) return;
        
        NotificationTimePicker.Visibility = Visibility.Visible;
    }

    /// <summary>
    /// Handle time picker value changed.
    /// </summary>
    private void NotificationTimePicker_TimeChanged(object sender, TimePickerValueChangedEventArgs e)
    {
        // Update the DateTime by combining date with new time
        ViewModel.NotificationTime = DateTime.Today.Add(e.NewTime);
        NotificationTimePicker.Visibility = Visibility.Collapsed;
    }

    /// <summary>
    /// Handle tap on sober date cell - shows calendar date picker.
    /// </summary>
    private void SoberDate_Tapped(object sender, TappedRoutedEventArgs e)
    {
        SoberDatePicker.Visibility = Visibility.Visible;
        SoberDatePicker.IsCalendarOpen = true;
    }

    /// <summary>
    /// Handle calendar date picker value changed.
    /// </summary>
    private void SoberDatePicker_DateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
    {
        if (args.NewDate.HasValue)
        {
            ViewModel.SoberDate = args.NewDate.Value.DateTime;
        }
        SoberDatePicker.Visibility = Visibility.Collapsed;
    }

    /// <summary>
    /// Handle tap on sober time display preference cell - shows combo box.
    /// </summary>
    private void SoberTimeDisplay_Tapped(object sender, TappedRoutedEventArgs e)
    {
        SoberTimeDisplayComboBox.Visibility = Visibility.Visible;
        SoberTimeDisplayComboBox.IsDropDownOpen = true;
    }

    /// <summary>
    /// Handle combo box selection changed.
    /// </summary>
    private void SoberTimeDisplayComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        SoberTimeDisplayComboBox.Visibility = Visibility.Collapsed;
    }
}
