using DailyReflection.Presentation.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;

namespace DailyReflection.Views;

/// <summary>
/// Page for managing app settings. The MVUX navigator assigns the DataContext
/// (generated <see cref="BindableSettingsModel"/>); the toggle and the two
/// ComboBox rows write the model's states via TwoWay bindings. The flyout-backed rows
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

    // MD dialogs cap at 560; below that the pickers span the tapped card.
    private const double MaxPickerFlyoutWidth = 560;

    private double _pickerFlyoutTargetWidth;

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

        _pickerFlyoutTargetWidth = ((FrameworkElement)sender).ActualWidth;
        NotificationTimeFlyout.Time = ViewModel.NotificationTime.TimeOfDay;
        NotificationTimeFlyout.ShowAt((FrameworkElement)sender);
    }

    private void PickerFlyout_Opened(object? sender, object e)
    {
        // The presenter is created by the flyout and only reachable once open;
        // stretch it to the width of the settings card the flyout came from.
        FrameworkElement? presenter =
            (FrameworkElement?)FindOpenPopupChild<TimePickerFlyoutPresenter>()
            ?? FindOpenPopupChild<DatePickerFlyoutPresenter>();
        if (presenter is not null && _pickerFlyoutTargetWidth > 0)
        {
            presenter.MinWidth = Math.Min(_pickerFlyoutTargetWidth, MaxPickerFlyoutWidth);
            presenter.Width = presenter.MinWidth;
        }
    }

    private T? FindOpenPopupChild<T>() where T : UIElement
    {
        foreach (var popup in VisualTreeHelper.GetOpenPopupsForXamlRoot(XamlRoot))
        {
            if (popup.Child is T child)
            {
                return child;
            }
        }

        return null;
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

        _pickerFlyoutTargetWidth = ((FrameworkElement)sender).ActualWidth;
        // The feed is None until the user picks a date (exposed as the DateTime
        // default); the picker opens on today in that case.
        var current = ViewModel.SoberDate;
        SoberDateFlyout.MaxYear = new DateTimeOffset(ViewModel.MaxDate);
        SoberDateFlyout.Date = new DateTimeOffset(current > DateTime.MinValue ? current : DateTime.Today);
        SoberDateFlyout.ShowAt((FrameworkElement)sender);
    }

    private void SoberDateFlyout_DatePicked(DatePickerFlyout sender, DatePickedEventArgs args)
    {
        if (ViewModel is null)
        {
            return;
        }

        // DatePickerFlyout can only bound the year (MaxYear), so clamp
        // day-level picks — the sober date cannot be in the future.
        var picked = args.NewDate.Date;
        ViewModel.SoberDate = picked > ViewModel.MaxDate ? ViewModel.MaxDate : picked;
    }

    private async void SupportMe_Tapped(object sender, TappedRoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = "Support Me!",
            Content = new TextBlock
            {
                Text = "I build and maintain this app in my free time. "
                    + "If it has been helpful to you, any support is greatly "
                    + "appreciated — but never expected. Thanks for being here!",
                TextWrapping = TextWrapping.Wrap,
            },
            PrimaryButtonText = "Buy me a coffee",
            CloseButtonText = "Maybe later",
            DefaultButton = ContentDialogButton.Primary,
        };

        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri("https://buymeacoffee.com/kazo0"));
        }
    }
}
