using DailyReflection.Presentation.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;

namespace DailyReflection.Views;

/// <summary>
/// Page for managing app settings. The MVUX navigator assigns the DataContext
/// (generated <see cref="BindableSettingsModel"/>); the toggle and ComboBox
/// write the model's states via TwoWay bindings. The flyout-backed rows
/// (time picker, date picker) have no binding channel, so their picked values
/// are written to the same states from these handlers — the code-behind
/// equivalent of a TwoWay binding write.
/// </summary>
public sealed partial class SettingsPage : Page
{
    public SettingsPage()
    {
        this.InitializeComponent();
    }

    private BindableSettingsModel? ViewModel => DataContext as BindableSettingsModel;

    private void NotificationTime_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }

        // Match the Xamarin TimePickerLabelEnabledConverter rule — Android always
        // taps through; iOS / desktop only when notifications are enabled.
        if (!OperatingSystem.IsAndroid() && !ViewModel.NotificationsEnabled)
        {
            return;
        }

        NotificationTimeFlyout.Time = ViewModel.NotificationTime.TimeOfDay;
        NotificationTimeFlyout.ShowAt((FrameworkElement)sender);
    }

    private void NotificationTimeFlyout_TimePicked(TimePickerFlyout sender, TimePickedEventArgs args)
    {
        if (ViewModel is null)
        {
            return;
        }

        // Spec 006 §B — preserve the date component on the persisted DateTime
        // instead of rebasing to today on every time change.
        var current = ViewModel.NotificationTime;
        ViewModel.NotificationTime = new DateTime(
            current.Year, current.Month, current.Day,
            args.NewTime.Hours, args.NewTime.Minutes, 0, current.Kind);
    }

    private void SoberDate_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }

        SoberDatePicker.MaxDate = new DateTimeOffset(ViewModel.MaxDate);
        // The feed is None until the user picks a date (exposed as the DateTime
        // default); the picker opens on today in that case.
        var current = ViewModel.SoberDate;
        SoberDatePicker.Date = new DateTimeOffset(current > DateTime.MinValue ? current : DateTime.Today);
        SoberDatePicker.Visibility = Visibility.Visible;
        SoberDatePicker.IsCalendarOpen = true;
    }

    private void SoberDatePicker_DateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
    {
        if (args.NewDate.HasValue && ViewModel is not null)
        {
            ViewModel.SoberDate = args.NewDate.Value.DateTime;
        }

        SoberDatePicker.Visibility = Visibility.Collapsed;
    }

    private void SoberTimeDisplay_Tapped(object sender, TappedRoutedEventArgs e)
    {
        SoberTimeDisplayComboBox.Visibility = Visibility.Visible;
        SoberTimeDisplayComboBox.IsDropDownOpen = true;
    }

    private void SoberTimeDisplayComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        SoberTimeDisplayComboBox.Visibility = Visibility.Collapsed;
    }
}
