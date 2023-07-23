using System.Globalization;

namespace DailyReflection.Converters
{
	public class PluralityConverter : IValueConverter
	{
		public string PluralValue { get; set; }
		public string SingularValue { get; set; }

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is int num)
			{
				return num == 1 ? SingularValue : PluralValue;
			}

			return null;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
