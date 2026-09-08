using Microsoft.UI.Xaml.Data;
using System;

namespace DailyReflection.Converters;

/// <summary>
/// Bridges the pickers' <see cref="DateTimeOffset"/> properties to the models'
/// <see cref="DateTime"/> states so a picker can be bound TwoWay instead of
/// writing the picked value back from a code-behind handler.
/// <para>
/// <see cref="ConvertBack"/> drops the offset (<see cref="DateTimeOffset.DateTime"/>),
/// which is exactly what the removed <c>DatePicked</c> handler persisted.
/// An unset date (<see cref="DateTime.MinValue"/>) converts to today rather
/// than throwing — applying a positive local offset to MinValue is out of range.
/// </para>
/// </summary>
public class DateTimeToDateTimeOffsetConverter : IValueConverter
{
	public object Convert(object? value, Type targetType, object? parameter, string language)
		=> value switch
		{
			DateTimeOffset offset => offset,
			DateTime date when date > DateTime.MinValue => new DateTimeOffset(date),
			_ => new DateTimeOffset(DateTime.Today),
		};

	public object ConvertBack(object? value, Type targetType, object? parameter, string language)
		=> value switch
		{
			DateTimeOffset offset => offset.DateTime,
			DateTime date => date,
			_ => DateTime.Today,
		};
}
