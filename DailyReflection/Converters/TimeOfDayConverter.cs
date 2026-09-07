using Microsoft.UI.Xaml.Data;

namespace DailyReflection.Converters;

/// <summary>
/// Settings model <see cref="DateTime"/> &lt;-&gt; TimePicker <c>SelectedTime</c>
/// (<see cref="TimeSpan"/>). Only the time of day travels: a picked time comes
/// back on today's date and <c>SettingsModel</c> restores the persisted date
/// component (spec 006 §B).
/// </summary>
public class TimeOfDayConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object? parameter, string language)
		=> value is DateTime d ? d.TimeOfDay : null;

	public object? ConvertBack(object? value, Type targetType, object? parameter, string language)
		=> value is TimeSpan time ? DateTime.Today.Add(time) : DependencyProperty.UnsetValue;
}
