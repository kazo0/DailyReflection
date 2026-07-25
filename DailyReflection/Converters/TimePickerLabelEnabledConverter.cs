using Microsoft.UI.Xaml.Data;
using System;

namespace DailyReflection.Converters;

/// <summary>
/// Spec 006 — restores the Xamarin `TimePickerLabelEnabledConverter`. On
/// Android the notification-time row is always tappable so the user can change
/// the *time* before flipping notifications on; on iOS / desktop the row
/// follows the bound bool (the page background-disables the row when
/// <c>NotificationsEnabled = false</c>).
/// </summary>
public class TimePickerLabelEnabledConverter : IValueConverter
{
	public object Convert(object? value, Type targetType, object? parameter, string language)
	{
		if (OperatingSystem.IsAndroid())
		{
			return true;
		}

		return value is bool b && b;
	}

	public object ConvertBack(object? value, Type targetType, object? parameter, string language)
		=> throw new NotImplementedException();
}
