using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace DailyReflection.Converters;

/// <summary>
/// Converter that converts bool to Visibility.
/// </summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; } = false;

    public object Convert(object? value, Type targetType, object? parameter, string language)
    {
        var boolValue = value is bool b && b;
        if (Invert)
        {
            boolValue = !boolValue;
        }
        return boolValue ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, string language)
    {
        throw new NotImplementedException();
    }
}
