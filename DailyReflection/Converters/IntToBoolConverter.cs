using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace DailyReflection.Converters;

/// <summary>
/// Converter that converts int to Visibility (Visible if non-zero).
/// Migrated from MAUI CommunityToolkit.Maui IntToBoolConverter to WinUI IValueConverter.
/// WinUI requires Visibility type for Visibility property bindings.
/// </summary>
public class IntToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, string language)
    {
        if (value is int intValue)
        {
            // Return appropriate type based on target
            if (targetType == typeof(Visibility))
            {
                return intValue != 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            return intValue != 0;
        }
        
        // Default returns based on target type
        if (targetType == typeof(Visibility))
        {
            return Visibility.Collapsed;
        }
        return false;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, string language)
    {
        throw new NotImplementedException();
    }
}
