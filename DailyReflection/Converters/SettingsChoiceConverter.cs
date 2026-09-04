using DailyReflection.Data.Models;
using Microsoft.UI.Xaml.Data;

namespace DailyReflection.Converters;

/// <summary>
/// Display text for the enum-backed Settings pickers (Sober Time Display, Theme).
/// One converter for both so <c>DRSettingsComboBoxStyle</c> (Styles/SettingsComboBox.xaml)
/// can render the selected value and the dropdown items itself, without each row
/// wiring its own templates. Strings are the Xamarin app's.
/// </summary>
public class SettingsChoiceConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, string language) => value switch
    {
        SoberTimeDisplayPreference.DaysMonthsYears => "Days, Months, and Years",
        SoberTimeDisplayPreference.DaysOnly => "Days Only",
        AppThemePreference.System => "System",
        AppThemePreference.Light => "Light",
        AppThemePreference.Dark => "Dark",
        _ => value?.ToString(),
    };

    public object? ConvertBack(object? value, Type targetType, object? parameter, string language)
    {
        throw new NotImplementedException();
    }
}
