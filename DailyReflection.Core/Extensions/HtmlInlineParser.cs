using System;
using System.Collections.Generic;
using System.Text;
using System.Web;

namespace DailyReflection.Core.Extensions;

/// <summary>
/// Token kind emitted by <see cref="HtmlInlineParser.Parse"/>.
/// </summary>
public enum HtmlInlineKind
{
	Text,
	LineBreak,
}

/// <summary>
/// One piece of inline content parsed from the reflection-database HTML.
/// </summary>
public readonly record struct HtmlInline(HtmlInlineKind Kind, string Text, bool Italic);

/// <summary>
/// Parses the small HTML markup the reflection database uses into a
/// platform-neutral sequence of <see cref="HtmlInline"/>. Lives in
/// <c>DailyReflection.Core</c> so unit tests can run without pulling in
/// WinUI / Uno types.
/// </summary>
/// <remarks>
/// Supported markup: <c>&lt;i&gt;…&lt;/i&gt;</c> (italics),
/// <c>&lt;br&gt;</c>/<c>&lt;br/&gt;</c> (line break), and
/// <c>&lt;p&gt;…&lt;/p&gt;</c> (paragraph break). Other tags the database
/// contains (<c>span</c>, <c>sup</c>, <c>a</c>, <c>table</c>…) are stripped
/// while their inner text flows through; consecutive paragraph breaks are
/// collapsed and edge breaks trimmed so block tags don't add stray space.
/// </remarks>
public static class HtmlInlineParser
{
	public static IEnumerable<HtmlInline> Parse(string? html)
	{
		var result = new List<HtmlInline>();
		var pendingBreaks = 0;

		foreach (var item in ParseRaw(html))
		{
			if (item.Kind == HtmlInlineKind.LineBreak)
			{
				pendingBreaks++;
				continue;
			}

			// Breaks only separate content: drop leading/trailing breaks and
			// collapse long runs to a single blank line.
			if (result.Count > 0 && pendingBreaks > 0)
			{
				var breaksToEmit = Math.Min(pendingBreaks, 2);
				for (var b = 0; b < breaksToEmit; b++)
				{
					result.Add(new HtmlInline(HtmlInlineKind.LineBreak, string.Empty, item.Italic));
				}
			}

			pendingBreaks = 0;
			result.Add(item);
		}

		return result;
	}

	private static IEnumerable<HtmlInline> ParseRaw(string? html)
	{
		if (string.IsNullOrEmpty(html))
		{
			yield break;
		}

		bool italic = false;
		int i = 0;
		var buffer = new StringBuilder();

		HtmlInline FlushText()
		{
			var text = HttpUtility.HtmlDecode(buffer.ToString());
			buffer.Clear();
			return new HtmlInline(HtmlInlineKind.Text, text, italic);
		}

		while (i < html.Length)
		{
			if (html[i] == '<')
			{
				int close = html.IndexOf('>', i + 1);
				if (close < 0)
				{
					// Unterminated tag — treat the rest as plain text.
					buffer.Append(html, i, html.Length - i);
					i = html.Length;
					continue;
				}

				var tag = html.AsSpan(i + 1, close - i - 1).ToString().Trim().ToLowerInvariant();
				var isClosing = tag.StartsWith('/');
				var name = tag.TrimStart('/').Split(' ', '/')[0];

				switch (name)
				{
					case "i":
						if (buffer.Length > 0)
						{
							yield return FlushText();
						}
						italic = !isClosing;
						break;
					case "br":
					case "p":
						// Block container — both the open and close of a <p>
						// break the flow; Parse() collapses the runs.
						if (buffer.Length > 0)
						{
							yield return FlushText();
						}
						yield return new HtmlInline(HtmlInlineKind.LineBreak, string.Empty, italic);
						break;
					case "tr" when isClosing:
						if (buffer.Length > 0)
						{
							yield return FlushText();
						}
						yield return new HtmlInline(HtmlInlineKind.LineBreak, string.Empty, italic);
						break;
					case "td" when isClosing:
						// Keep table cells from merging into one word.
						buffer.Append(' ');
						break;
					default:
						// Strip unknown/unsupported tags (span, sup, a, table,
						// tbody, b, em, …) — their inner text flows through as
						// plain text instead of leaking raw markup into the UI.
						break;
				}

				i = close + 1;
			}
			else
			{
				buffer.Append(html[i]);
				i++;
			}
		}

		if (buffer.Length > 0)
		{
			yield return FlushText();
		}
	}
}
