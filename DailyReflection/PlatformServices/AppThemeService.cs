using DailyReflection.Data.Models;
using DailyReflection.Services.Theme;
using Microsoft.UI.Xaml;
using Uno.Toolkit.UI;
using Windows.UI.ViewManagement;

namespace DailyReflection.PlatformServices;

/// <summary>
/// Theme service for the Uno head. Sets <see cref="FrameworkElement.RequestedTheme"/>
/// on the window's root element — the same mechanism as Uno Toolkit's
/// <c>SystemThemeHelper.SetApplicationTheme(XamlRoot, ElementTheme)</c>.
/// <para>
/// The root always carries an explicit <see cref="ElementTheme.Light"/> or
/// <see cref="ElementTheme.Dark"/>; <see cref="AppThemePreference.System"/> is
/// resolved to the current OS theme and re-applied whenever the OS theme
/// changes (<see cref="UISettings.ColorValuesChanged"/>, which Uno raises from
/// its OS-theme hook). Resetting the root to <see cref="ElementTheme.Default"/>
/// instead was tried and rejected: once the root has carried an explicit theme,
/// Uno leaves style-supplied ThemeResources (the TitleLarge / BodyMedium
/// foregrounds) stale on the next OS theme change (observed on Skia macOS),
/// whereas explicit sets re-theme the whole tree reliably.
/// </para>
/// <para>
/// The root element only exists once navigation has populated the window, so
/// <c>App.OnLaunched</c> applies the persisted preference right after
/// <c>NavigateAsync</c>; later changes come from <c>SettingsModel</c>.
/// </para>
/// </summary>
public class AppThemeService : IAppThemeService
{
    // Kept as a field: the event source must stay alive for the subscription.
    private readonly UISettings _uiSettings = new();
    private AppThemePreference _preference = AppThemePreference.System;

    public AppThemeService()
    {
        _uiSettings.ColorValuesChanged += OnColorValuesChanged;
    }

    public void ApplyTheme(AppThemePreference preference)
    {
        _preference = preference;
        Dispatch(Apply);
    }

    private void OnColorValuesChanged(UISettings sender, object args)
    {
        if (_preference == AppThemePreference.System)
        {
            Dispatch(Apply);
        }
    }

    /// <summary>
    /// Runs <paramref name="action"/> on the UI thread — MVUX ForEach side
    /// effects and the OS-theme event may arrive on other threads.
    /// </summary>
    private static void Dispatch(Action action)
    {
        var window = (Application.Current as App)?.MainWindow;
        if (window is null)
        {
            return;
        }

        var dispatcher = window.DispatcherQueue;
        if (dispatcher.HasThreadAccess)
        {
            action();
        }
        else
        {
            dispatcher.TryEnqueue(() => action());
        }
    }

    private void Apply()
    {
        var window = (Application.Current as App)?.MainWindow;
        if (window?.Content is FrameworkElement root)
        {
            root.RequestedTheme = ToElementTheme(_preference);
        }
    }

    internal static ElementTheme ToElementTheme(AppThemePreference preference) => preference switch
    {
        AppThemePreference.Light => ElementTheme.Light,
        AppThemePreference.Dark => ElementTheme.Dark,
        _ => SystemThemeHelper.GetCurrentOsTheme() == ApplicationTheme.Dark
            ? ElementTheme.Dark
            : ElementTheme.Light,
    };
}
