using DailyReflection.Presentation.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.ComponentModel;
using Uno.Toolkit.UI;

namespace DailyReflection.Views;

/// <summary>
/// Page displaying the daily reflection content. The MVUX navigator assigns
/// the DataContext (generated <see cref="BindableDailyReflectionModel"/>); the
/// Reflections list feed drives the FeedView/FlipView in XAML.
///
/// The one piece of plumbing here is keeping the FlipView's position and the
/// model's <c>SelectedIndex</c> in step. MVUX's <c>Selection()</c> operator
/// only bridges <c>ListViewBase</c> (via <c>ISelectionInfo</c>), and a TwoWay
/// binding on <c>FlipView.SelectedIndex</c> is unsafe: the FlipView auto-selects
/// item 0 the moment its ItemsSource lands, which a binding would push into the
/// model as "the user paged to January 1st". So the sync is wired up only once
/// the FlipView is loaded, after its position has been set from the model.
/// </summary>
public sealed partial class DailyReflectionPage : Page
{
    /// <summary>Name of the PipsPager template part holding the pips.</summary>
    private const string PipsRepeaterName = "PipsPagerItemsRepeater";

    private FlipView? _flipView;

    public DailyReflectionPage()
    {
        this.InitializeComponent();
    }

    private BindableDailyReflectionModel? ViewModel => DataContext as BindableDailyReflectionModel;

    private void DatePickerFlyout_DatePicked(DatePickerFlyout sender, DatePickedEventArgs args)
    {
        // Flyouts have no binding channel; writing the generated VM's Date
        // property is the two-way-binding write (it forwards to the IState).
        // The reflection feed and SelectedIndex reload off the state change,
        // which moves the FlipView (see ViewModel_PropertyChanged).
        if (ViewModel is { } viewModel)
        {
            viewModel.Date = args.NewDate.DateTime;
        }
    }

    private void ReflectionsFlipView_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is not FlipView flipView || ViewModel is not { } viewModel)
        {
            return;
        }

        _flipView = flipView;

        // Position first, listen second: the FlipView's own initial selection
        // (item 0) must never reach the model as a page change.
        ApplySelectedIndex();
        flipView.SelectionChanged += ReflectionsFlipView_SelectionChanged;
        viewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    private void ReflectionsFlipView_Unloaded(object sender, RoutedEventArgs e)
    {
        if (sender is FlipView flipView)
        {
            flipView.SelectionChanged -= ReflectionsFlipView_SelectionChanged;
        }

        if (ViewModel is { } viewModel)
        {
            viewModel.PropertyChanged -= ViewModel_PropertyChanged;
        }

        _flipView = null;
    }

    /// <summary>
    /// Keeps vertical wheel / touchpad scrolling from paging the FlipView.
    /// <para>
    /// The FlipView flips on any wheel tick that reaches it, and one reaches
    /// it whenever the reading's ScrollViewer has nothing left to scroll (a
    /// reading that fits, or scrolled to its top/bottom) — so every reading
    /// ended with an accidental page turn. Handled here, between the
    /// ScrollViewer (which has already scrolled if it could) and the FlipView.
    /// Horizontal wheel is left alone: a sideways touchpad swipe still pages.
    /// </para>
    /// </summary>
    private void Reading_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        => e.Handled = !e.GetCurrentPoint(null).Properties.IsHorizontalMouseWheel;

    /// <summary>User paged (swipe, arrow, or pip): push the new page onto the model.</summary>
    private void ReflectionsFlipView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is FlipView { SelectedIndex: >= 0 } flipView
            && ViewModel is { } viewModel
            && viewModel.SelectedIndex != flipView.SelectedIndex)
        {
            viewModel.SelectedIndex = flipView.SelectedIndex;
        }
    }

    /// <summary>Model moved (date picked, or the initial value arriving): move the FlipView.</summary>
    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(BindableDailyReflectionModel.SelectedIndex))
        {
            ApplySelectedIndex();
        }
    }

    private void ApplySelectedIndex()
    {
        if (_flipView is not { } flipView || ViewModel is not { } viewModel)
        {
            return;
        }

        var index = viewModel.SelectedIndex;
        if (index < 0 || index >= flipView.Items.Count || flipView.SelectedIndex == index)
        {
            return;
        }

        // A programmatic move can span hundreds of pages (Jan 1 → today on
        // startup, or a date pick); jump rather than animate through them.
        var animate = flipView.UseTouchAnimationsForAllNavigation;
        flipView.UseTouchAnimationsForAllNavigation = false;
        try
        {
            flipView.SelectedIndex = index;
        }
        finally
        {
            flipView.UseTouchAnimationsForAllNavigation = animate;
        }

        DispatcherQueue.TryEnqueue(() => ScrollPagerToSelectedPip(flipView, index));
    }

    /// <summary>
    /// Scrolls the linked PipsPager to the selected pip.
    /// <para>
    /// The pager keeps up with paging by itself, but not with a programmatic
    /// jump: it brings the selected pip into view from the SelectedPageIndex
    /// change, and on a jump that pip is realized in the very same pass, before
    /// the pager's inner ScrollViewer has taken the extent the new pips give it.
    /// The request is dropped, so the pager stays at offset 0 while every
    /// realized pip sits off-viewport — 366 pips wide, that means an empty gap
    /// between the arrows until the next page change (observed on Skia desktop
    /// with Uno 6.8). Re-issuing the request a dispatcher turn later, once
    /// layout has settled, lands it.
    /// </para>
    /// </summary>
    private static void ScrollPagerToSelectedPip(FlipView flipView, int index)
    {
        if (flipView.SelectedIndex != index
            || SelectorExtensions.GetPipsPager(flipView) is not { } pager
            || FindDescendant<ItemsRepeater>(pager, PipsRepeaterName) is not { } repeater
            || repeater.TryGetElement(index) is not { } pip)
        {
            return;
        }

        pip.StartBringIntoView(new BringIntoViewOptions
        {
            HorizontalAlignmentRatio = 0.5,
            AnimationDesired = false,
        });
    }

    private static T? FindDescendant<T>(DependencyObject root, string name)
        where T : FrameworkElement
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T match && match.Name == name)
            {
                return match;
            }

            if (FindDescendant<T>(child, name) is { } descendant)
            {
                return descendant;
            }
        }

        return null;
    }
}
