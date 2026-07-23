using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace DailyReflection.Converters;

/// <summary>
/// DateTime → Visibility: Visible only when the value is a real date (after
/// <see cref="DateTime.MinValue"/>). Replaces NullToBoolConverter for the
/// sober-date bindings now that the MVUX bindable exposes an unset SoberDate
/// (feed None) as the DateTime default instead of null.
/// </summary>
public class DateSetToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, string language)
        => value is DateTime d && d > DateTime.MinValue ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object? value, Type targetType, object? parameter, string language)
        => throw new NotImplementedException();
}
