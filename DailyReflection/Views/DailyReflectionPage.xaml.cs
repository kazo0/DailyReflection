using DailyReflection.Converters;
using DailyReflection.Presentation.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.ComponentModel;

namespace DailyReflection.Views;

/// <summary>
/// Page displaying the daily reflection content.
/// Lifecycle / VM activation are owned by <see cref="PageBase"/>.
/// </summary>
public sealed partial class DailyReflectionPage : PageBase
{
    public DailyReflectionViewModel ViewModel { get; }
    protected override ViewModelBase ActiveViewModel => ViewModel;

    public DailyReflectionPage()
    {
        ViewModel = App.GetService<DailyReflectionViewModel>();
        DataContext = ViewModel;

        this.InitializeComponent();

        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        RefreshInlines();
    }

    protected override async void OnPageLoaded()
    {
        // Init() is idempotent — subsequent navigations to this page do not refetch.
        await ViewModel.Init();
    }

    protected override void OnPageUnloaded()
    {
        ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DailyReflectionViewModel.DailyReflection))
        {
            RefreshInlines();
        }
    }

    private void RefreshInlines()
    {
        var reflection = ViewModel.DailyReflection;
        SetInlines(TitleText, reflection?.Title);
        SetInlines(ReadingText, reflection?.Reading);
        SetInlines(ThoughtText, reflection?.Thought);
    }

    private static void SetInlines(TextBlock target, string? html)
    {
        target.Inlines.Clear();
        if (string.IsNullOrEmpty(html))
        {
            return;
        }

        foreach (var inline in HtmlToInlinesConverter.ParseInlines(html))
        {
            target.Inlines.Add(inline);
        }
    }

    public Visibility ShowContent(bool hasError, bool isLoading)
        => (!hasError && !isLoading) ? Visibility.Visible : Visibility.Collapsed;

    public string FormatPageDate(DateTime date) => date.ToString("MMMM d");

    private async void DatePickerFlyout_DatePicked(DatePickerFlyout sender, DatePickedEventArgs args)
    {
        ViewModel.Date = args.NewDate.DateTime;

        if (ViewModel.GetDailyReflectionCommand.CanExecute(null))
        {
            await ViewModel.GetDailyReflectionCommand.ExecuteAsync(null);
        }
    }
}
