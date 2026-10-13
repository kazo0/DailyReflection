using DailyReflection.Core.Entities;
using Microsoft.UI.Xaml.Data;

namespace DailyReflection.Converters;

/// <summary>
/// Converts a <see cref="ReadingTextSize"/> to the label the Settings
/// picker shows for it (spec 013 §A).
/// </summary>
public class ReadingTextSizeEnumConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object? parameter, string language)
	{
		if (value is ReadingTextSize size)
		{
			return size switch
			{
				ReadingTextSize.Normal => "Normal",
				ReadingTextSize.Large => "Large",
				ReadingTextSize.ExtraLarge => "Extra Large",
				ReadingTextSize.Huge => "Huge",
				_ => null,
			};
		}

		return null;
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, string language)
	{
		throw new NotImplementedException();
	}
}
