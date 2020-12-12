using DailyReflection.Data.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Xamarin.Forms;

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
