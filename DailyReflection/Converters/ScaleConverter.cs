using Microsoft.UI.Xaml.Data;
using System.Globalization;

namespace DailyReflection.Converters;

/// <summary>
/// Multiplies a base value, given as the ConverterParameter (a number, e.g.
/// <c>ConverterParameter=17</c>), by the bound scale factor. The Reflection
/// tab sizes its text and its column with it from the model's TextScale
/// (spec 013 §B), the Sober Time tab likewise (§D). Until the scale is bound
/// (null, or the generated view model's 0 before the feed's first value) the
/// base value is returned as it is, so the page never flashes at the theme's
/// size or at size 0.
/// </summary>
public class ScaleConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object? parameter, string language)
	{
		var baseValue = System.Convert.ToDouble(parameter, CultureInfo.InvariantCulture);
		return value is double scale && scale > 0 ? baseValue * scale : baseValue;
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, string language)
	{
		throw new NotImplementedException();
	}
}
