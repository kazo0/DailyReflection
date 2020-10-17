using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Xamarin.Forms;

namespace DailyReflection.Converters
{
	public class DateTimeToTimeSpanConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is DateTime dateTime)
			{
				return dateTime.TimeOfDay;
			}

			return null;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is TimeSpan time)
			{
				return DateTime.MinValue.Add(time);
			}

			return null;
		}
	}
}
