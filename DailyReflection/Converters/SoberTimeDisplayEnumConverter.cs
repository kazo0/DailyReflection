using DailyReflection.Data.Models;
using Microsoft.UI.Xaml.Data;

namespace DailyReflection.Converters;

/// <summary>
/// Converter that converts SoberTimeDisplayPreference enum to display string.
/// Migrated from MAUI IValueConverter to WinUI IValueConverter.
/// </summary>
public class SoberTimeDisplayEnumConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, string language)
    {
        if (value is SoberTimeDisplayPreference displayPref)
        {
            return displayPref switch
            {
                SoberTimeDisplayPreference.DaysMonthsYears => "Days, Months, and Years",
                SoberTimeDisplayPreference.DaysOnly => "Days Only",
                _ => null,
            };
        }

        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, string language)
    {
        throw new NotImplementedException();
    }
}
