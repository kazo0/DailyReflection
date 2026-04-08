using Microsoft.UI.Xaml.Data;

namespace DailyReflection.Converters;

/// <summary>
/// Converter that converts DateTime to TimeSpan and back.
/// Migrated from MAUI IValueConverter to WinUI IValueConverter.
/// </summary>
public class DateTimeToTimeSpanConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, string language)
    {
        if (value == null)
        {
            return null;
        }

        if (value is DateTime dateTime)
        {
            return dateTime.TimeOfDay;
        }

        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, string language)
    {
        if (value is TimeSpan time)
        {
            return DateTime.MinValue.Add(time);
        }

        return null;
    }
}
