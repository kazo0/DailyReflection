using DailyReflection.Data.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Xamarin.Forms;

namespace DailyReflection.Converters
{
	public class SoberTimeDisplayEnumConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is SoberTimeDisplayPreference displayPref)
			{
				return displayPref switch
				{
					SoberTimeDisplayPreference.DaysMonthsYears => "Days, Months, and Years",
					SoberTimeDisplayPreference.DaysOnly => "Days Only",
					_ => null,
				};
			}

			return null;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
