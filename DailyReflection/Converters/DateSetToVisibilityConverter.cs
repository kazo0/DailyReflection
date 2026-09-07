using Microsoft.UI.Xaml.Data;

namespace DailyReflection.Converters;

/// <summary>
/// Date → Visibility: Visible only when the value is a real date. The MVUX
/// bindable exposes an unset SoberDate (feed None) as null or the type's
/// default, both of which collapse.
/// </summary>
public class DateSetToVisibilityConverter : IValueConverter
{
	public object Convert(object? value, Type targetType, object? parameter, string language)
		=> value switch
		{
			DateTimeOffset o when o > DateTimeOffset.MinValue => Visibility.Visible,
			DateTime d when d > DateTime.MinValue => Visibility.Visible,
			_ => Visibility.Collapsed,
		};

	public object ConvertBack(object? value, Type targetType, object? parameter, string language)
		=> throw new NotImplementedException();
}
