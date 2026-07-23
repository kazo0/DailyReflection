using CommunityToolkit.Mvvm.Messaging;
using DailyReflection.Presentation.Messages;
using DailyReflection.Presentation.ViewModels;
using DailyReflection.Services.VersionTracking;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Threading.Tasks;

namespace DailyReflection.Views;

/// <summary>
/// Page for managing app settings. Lifecycle, messenger registration, and
/// VM activation are inherited from <see cref="PageBase{TViewModel}"/>.
/// </summary>
public sealed partial class SettingsPage : PageBase
{
    public SettingsViewModel ViewModel { get; }
    protected override ViewModelBase ActiveViewModel => ViewModel;

    /// <summary>
    /// Runtime app version sourced from <see cref="IVersionTrackingService"/>
    /// (spec 001 — replaces the hard-coded VersionConstants.VersionNumber).
    /// </summary>
    public string AppVersion { get; }

    public SettingsPage()
    {
        ViewModel = App.GetService<SettingsViewModel>();
        DataContext = ViewModel;

        var version = App.GetService<IVersionTrackingService>();
        AppVersion = $"{version.CurrentVersion} ({version.CurrentBuild})";

        this.InitializeComponent();
    }

    protected override void RegisterMessages()
    {
        WeakReferenceMessenger.Default.Register<SettingsPage, NotificationPermissionRequestMessage>(
            this,
            (r, m) => m.Reply(r.ShowPermissionDialogAsync()));
    }

    private async Task<bool> ShowPermissionDialogAsync()
    {
        var dialog = new ContentDialog
        {
            Title = "Permission Required",
            Content = "In order for notifications to work, permission must be granted in the System Settings. Press OK to be brought to your system's Notification Settings page.",
            PrimaryButtonText = "OK",
            CloseButtonText = "Cancel",
            XamlRoot = this.XamlRoot,
        };

        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary;
    }

    public string FormatNotificationTime(DateTime time) => time.ToString("h:mm tt");
    public string FormatSoberDate(DateTime date) => date.ToString("MMM d, yyyy");

    private void NotificationTime_Tapped(object sender, TappedRoutedEventArgs e)
    {
        // Match the Xamarin TimePickerLabelEnabledConverter rule — Android always
        // taps through; iOS / desktop only when notifications are enabled.
        if (!OperatingSystem.IsAndroid() && !ViewModel.NotificationsEnabled)
        {
            return;
        }

        NotificationTimePicker.Visibility = Visibility.Visible;
    }

    private void NotificationTimePicker_TimeChanged(object sender, TimePickerValueChangedEventArgs e)
    {
        // Spec 006 §B — preserve the date component on the persisted DateTime
        // instead of rebasing to today on every time change.
        var current = ViewModel.NotificationTime;
        ViewModel.NotificationTime = new DateTime(
            current.Year, current.Month, current.Day,
            e.NewTime.Hours, e.NewTime.Minutes, 0, current.Kind);
        NotificationTimePicker.Visibility = Visibility.Collapsed;
    }

    private void SoberDate_Tapped(object sender, TappedRoutedEventArgs e)
    {
        SoberDatePicker.Visibility = Visibility.Visible;
        SoberDatePicker.IsCalendarOpen = true;
    }

    private void SoberDatePicker_DateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
    {
        if (args.NewDate.HasValue)
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
