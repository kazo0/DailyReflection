using DailyReflection.Presentation.Models;
using Microsoft.UI.Xaml.Controls;
using System;

namespace DailyReflection.Views;

/// <summary>
/// Page displaying the daily reflection content. The MVUX navigator assigns
/// the DataContext (generated <see cref="BindableDailyReflectionModel"/>); the
/// reflection feed drives the FeedView in XAML — no code-behind attach or
/// init plumbing is needed anymore.
/// </summary>
public sealed partial class DailyReflectionPage : Page
{
    public DailyReflectionPage()
    {
        this.InitializeComponent();
    }

    private void DatePickerFlyout_DatePicked(DatePickerFlyout sender, DatePickedEventArgs args)
    {
        // Flyouts have no binding channel; writing the generated VM's Date
        // property is the two-way-binding write (it forwards to the IState).
        // The reflection feed reloads off the state change automatically.
        if (DataContext is BindableDailyReflectionModel viewModel)
        {
            viewModel.Date = args.NewDate.DateTime;
        }
    }
}
