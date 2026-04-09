using Microsoft.UI.Xaml.Data;

namespace DailyReflection.Converters;

/// <summary>
/// Converter that returns singular or plural form based on numeric value.
/// Migrated from MAUI IValueConverter to WinUI IValueConverter.
/// </summary>
public class PluralityConverter : IValueConverter
{
    public string PluralValue { get; set; } = string.Empty;
    public string SingularValue { get; set; } = string.Empty;

    public object? Convert(object? value, Type targetType, object? parameter, string language)
    {
        if (value is int num)
        {
            return num == 1 ? SingularValue : PluralValue;
        }

        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, string language)
    {
        throw new NotImplementedException();
    }
}
