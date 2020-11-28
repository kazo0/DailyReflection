using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Xamarin.Forms;

namespace DailyReflection.Converters
{
	public class AllFalseMultiConverter : IMultiValueConverter
	{
		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			if (values == null || !targetType.IsAssignableFrom(typeof(bool)))
			{
				return BindableProperty.UnsetValue;
			}

			foreach (var value in values)
			{
				if (!(value is bool b))
				{
					return BindableProperty.UnsetValue;
				}
				else if (b)
				{
					return false;
				}
			}
			return true;
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
