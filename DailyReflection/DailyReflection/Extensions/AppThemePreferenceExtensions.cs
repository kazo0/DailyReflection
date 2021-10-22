using DailyReflection.Presentation.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace DailyReflection.Extensions
{
    public static class AppThemePreferenceExtensions
    {
        public static OSAppTheme ToOSAppTheme(this AppThemePreference value) => value switch
        {
            AppThemePreference.Light => OSAppTheme.Light,
            AppThemePreference.Dark => OSAppTheme.Dark,
            AppThemePreference.System => OSAppTheme.Unspecified,
            _ => OSAppTheme.Unspecified,
        };
    }
}
