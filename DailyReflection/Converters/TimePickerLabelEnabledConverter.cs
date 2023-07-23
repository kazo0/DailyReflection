using System.Globalization;

namespace DailyReflection.Converters
{
	public class TimePickerLabelEnabledConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (DeviceInfo.Platform == DevicePlatform.Android)
			{
				return true;
			}
			return (bool)value;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
