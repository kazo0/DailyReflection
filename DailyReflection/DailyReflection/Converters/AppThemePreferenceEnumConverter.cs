using DailyReflection.Presentation.Entities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Xamarin.Forms;

namespace DailyReflection.Converters
{
	public class AppThemePreferenceEnumConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is AppThemePreference themePref)
			{
				return themePref switch
				{
					AppThemePreference.System => "System",
					AppThemePreference.Light => "Light",
					AppThemePreference.Dark => "Dark",
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
