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
/// Parses the small HTML markup the reflection database uses
/// (<c>&lt;i&gt;…&lt;/i&gt;</c>, <c>&lt;br&gt;</c>, <c>&lt;br/&gt;</c>) into a
/// platform-neutral sequence of <see cref="HtmlInline"/>. Lives in
/// <c>DailyReflection.Core</c> so unit tests can run without pulling in
/// WinUI / Uno types.
/// </summary>
public static class HtmlInlineParser
{
	public static IEnumerable<HtmlInline> Parse(string? html)
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
				switch (tag)
				{
					case "i":
						if (buffer.Length > 0)
						{
							yield return FlushText();
						}
						italic = true;
						break;
					case "/i":
						if (buffer.Length > 0)
						{
							yield return FlushText();
						}
						italic = false;
						break;
					case "br":
					case "br/":
					case "br /":
						if (buffer.Length > 0)
						{
							yield return FlushText();
						}
						yield return new HtmlInline(HtmlInlineKind.LineBreak, string.Empty, italic);
						break;
					default:
						// Preserve unknown tags as literal text so a future markup
						// extension surfaces visibly instead of being silently dropped.
						buffer.Append(html, i, close - i + 1);
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
