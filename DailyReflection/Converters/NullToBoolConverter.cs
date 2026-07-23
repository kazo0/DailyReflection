using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace DailyReflection.Converters;

/// <summary>
/// Converter that returns Visibility.Visible if value is not null.
/// Migrated from MAUI CommunityToolkit.Maui IsNotNullConverter to WinUI IValueConverter.
/// WinUI requires Visibility type for Visibility property bindings.
/// </summary>
public class NullToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, string language)
    {
        bool isNotNull = value != null;
        
        // Return appropriate type based on target
        if (targetType == typeof(Visibility))
        {
            return isNotNull ? Visibility.Visible : Visibility.Collapsed;
        }
        return isNotNull;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, string language)
    {
        throw new NotImplementedException();
    }
}
