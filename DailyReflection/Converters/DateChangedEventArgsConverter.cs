
using System.Globalization;

namespace DailyReflection.Converters
{
	public class DateChangedEventArgsConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
			=> value is DateChangedEventArgs dateChangedEventArgs
			   ? dateChangedEventArgs.NewDate
			   : throw new ArgumentException("Expected value to be of type DateChangedEventArgs", nameof(value));

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
