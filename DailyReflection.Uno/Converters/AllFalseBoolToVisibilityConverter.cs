using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace DailyReflection.Converters;

/// <summary>
/// Converter that returns Visible if ALL input values are false, otherwise Collapsed.
/// Replaces MAUI's IMultiValueConverter with a simpler single-value approach.
/// For multi-binding scenarios in WinUI, you may need to use x:Bind with function binding.
/// </summary>
public class AllFalseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, string language)
    {
        if (value is bool boolValue)
        {
            return !boolValue ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, string language)
    {
        throw new NotImplementedException();
    }
}
