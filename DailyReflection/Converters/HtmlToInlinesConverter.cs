using DailyReflection.Core.Extensions;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Documents;
using System.Collections.Generic;

namespace DailyReflection.Converters;

/// <summary>
/// WinUI bridge from <see cref="HtmlInlineParser"/> output to <see cref="Inline"/>
/// elements. The parser itself is tested in <c>DailyReflection.Core</c>; this
/// class is purely a UI-side adapter.
/// </summary>
public class HtmlToInlinesConverter : IValueConverter
{
	public object? Convert(object? value, System.Type targetType, object? parameter, string language)
	{
		var list = new List<Inline>();
		foreach (var inline in ParseInlines(value as string))
		{
			list.Add(inline);
		}
		return list;
	}

	public object? ConvertBack(object? value, System.Type targetType, object? parameter, string language)
		=> throw new System.NotImplementedException();

	/// <summary>
	/// Materialises WinUI <see cref="Inline"/> elements from the platform-neutral
	/// <see cref="HtmlInlineParser"/> tokens. Used directly from
	/// <c>DailyReflectionPage</c>.<c>SetInlines</c>.
	/// </summary>
	public static IEnumerable<Inline> ParseInlines(string? html)
	{
		foreach (var token in HtmlInlineParser.Parse(html))
		{
			if (token.Kind == HtmlInlineKind.LineBreak)
			{
				yield return new LineBreak();
			}
			else
			{
				yield return new Run
				{
					Text = token.Text,
					FontStyle = token.Italic
						? Windows.UI.Text.FontStyle.Italic
						: Windows.UI.Text.FontStyle.Normal,
				};
			}
		}
	}
}
