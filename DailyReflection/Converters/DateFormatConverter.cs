using Microsoft.UI.Xaml.Data;
using System;

namespace DailyReflection.Converters;

/// <summary>
/// Formats a DateTime with a configurable format string (replaces the
/// per-page Format* x:Bind functions, which classic {Binding} cannot call).
/// Null or <see cref="DateTime.MinValue"/> input formats today when
/// <see cref="FallbackToToday"/> is set (the Settings sober-date row and
/// picker default), otherwise yields an empty string (the unset sober date).
/// </summary>
public class DateFormatConverter : IValueConverter
{
    public string Format { get; set; } = "d";

    public bool FallbackToToday { get; set; }

    public object Convert(object? value, Type targetType, object? parameter, string language)
    {
        var date = value switch
        {
            DateTime d when d > DateTime.MinValue => d,
            _ when FallbackToToday => DateTime.Today,
            _ => (DateTime?)null,
        };

        return date?.ToString(Format) ?? string.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, string language)
        => throw new NotImplementedException();
}
