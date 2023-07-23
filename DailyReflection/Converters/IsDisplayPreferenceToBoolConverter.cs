using DailyReflection.Data.Models;
using System.Globalization;

namespace DailyReflection.Converters
{
	public class IsDisplayPreferenceToBoolConverter : IValueConverter
	{
		public SoberTimeDisplayPreference DisplayPreference { get; set; }

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is SoberTimeDisplayPreference @enum)
			{
				return @enum == DisplayPreference;
			}

			return false;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
