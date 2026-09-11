using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace DailyReflection.Converters;

/// <summary>
/// Collapses an element when its bound string is null or whitespace.
/// </summary>
public class EmptyStringToVisibilityConverter : IValueConverter
{
	public object Convert(object? value, Type targetType, object? parameter, string language)
		=> string.IsNullOrWhiteSpace(value as string) ? Visibility.Collapsed : Visibility.Visible;

	public object ConvertBack(object? value, Type targetType, object? parameter, string language)
		=> throw new NotImplementedException();
}
