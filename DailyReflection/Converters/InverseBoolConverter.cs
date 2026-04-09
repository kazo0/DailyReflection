using Microsoft.UI.Xaml.Data;

namespace DailyReflection.Converters;

/// <summary>
/// Converter that inverts a boolean value.
/// Migrated from MAUI IValueConverter to WinUI IValueConverter.
/// </summary>
public class InverseBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, string language)
    {
        if (value is bool boolValue)
        {
            return !boolValue;
        }
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, string language)
    {
        throw new NotImplementedException();
    }
}
