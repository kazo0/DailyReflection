using DailyReflection.Core.Extensions;
using NUnit.Framework;
using System.Linq;

namespace DailyReflection.Services.Tests.Extensions;

[TestFixture]
public class HtmlInlineParserTests
{
	[Test]
	public void Plain_text_returns_one_upright_run()
	{
		var result = HtmlInlineParser.Parse("Hello, world.").ToList();

		Assert.That(result, Has.Count.EqualTo(1));
		Assert.That(result[0].Kind, Is.EqualTo(HtmlInlineKind.Text));
		Assert.That(result[0].Text, Is.EqualTo("Hello, world."));
		Assert.That(result[0].Italic, Is.False);
	}

	[Test]
	public void Italic_only_returns_one_italic_run()
	{
		var result = HtmlInlineParser.Parse("<i>quote</i>").ToList();

		Assert.That(result, Has.Count.EqualTo(1));
		Assert.That(result[0].Italic, Is.True);
		Assert.That(result[0].Text, Is.EqualTo("quote"));
	}

	[Test]
	public void Mixed_emphasis_yields_three_runs()
	{
		var result = HtmlInlineParser.Parse("Read <i>Daily Reflections</i> today.").ToList();

		Assert.That(result, Has.Count.EqualTo(3));

		Assert.That(result[0].Italic, Is.False);
		Assert.That(result[0].Text, Is.EqualTo("Read "));

		Assert.That(result[1].Italic, Is.True);
		Assert.That(result[1].Text, Is.EqualTo("Daily Reflections"));

		Assert.That(result[2].Italic, Is.False);
		Assert.That(result[2].Text, Is.EqualTo(" today."));
	}

	[Test]
	public void Br_emits_line_break()
	{
		var result = HtmlInlineParser.Parse("Line one<br>Line two").ToList();

		Assert.That(result, Has.Count.EqualTo(3));
		Assert.That(result[0].Text, Is.EqualTo("Line one"));
		Assert.That(result[1].Kind, Is.EqualTo(HtmlInlineKind.LineBreak));
		Assert.That(result[2].Text, Is.EqualTo("Line two"));
	}

	[Test]
	public void Self_closing_br_is_treated_as_break()
	{
		var result = HtmlInlineParser.Parse("a<br/>b").ToList();
		Assert.That(result.Select(r => r.Kind), Is.EqualTo(new[]
		{
			HtmlInlineKind.Text,
			HtmlInlineKind.LineBreak,
			HtmlInlineKind.Text,
		}));
	}

	[Test]
	public void Html_entities_are_decoded()
	{
		var result = HtmlInlineParser.Parse("Copyright &copy; 1990 &amp; later").ToList();

		Assert.That(result, Has.Count.EqualTo(1));
		Assert.That(result[0].Text, Is.EqualTo("Copyright © 1990 & later"));
	}

	[Test]
	public void Unterminated_tag_falls_through_as_text()
	{
		var result = HtmlInlineParser.Parse("trailing <i tag without close").ToList();

		Assert.That(result, Has.Count.EqualTo(1));
		Assert.That(result[0].Text, Is.EqualTo("trailing <i tag without close"));
		Assert.That(result[0].Italic, Is.False);
	}

	[Test]
	public void Unknown_tags_are_preserved_verbatim()
	{
		// Future proofing: a <b>…</b> in the DB should survive visibly so
		// it's noticed instead of silently dropped.
		var result = HtmlInlineParser.Parse("plain <b>bold?</b>").ToList();

		Assert.That(string.Concat(result.Select(r => r.Text)), Does.Contain("<b>bold?</b>"));
	}

	[Test]
	public void Empty_input_yields_no_inlines()
	{
		Assert.That(HtmlInlineParser.Parse(string.Empty).ToList(), Is.Empty);
		Assert.That(HtmlInlineParser.Parse(null).ToList(), Is.Empty);
	}

	[Test]
	public void Italic_with_inner_break()
	{
		var result = HtmlInlineParser.Parse("<i>line a<br/>line b</i>").ToList();

		Assert.That(result.Select(r => (r.Kind, r.Italic, r.Text)),
			Is.EqualTo(new[]
			{
				(HtmlInlineKind.Text,      true,  "line a"),
				(HtmlInlineKind.LineBreak, true,  string.Empty),
				(HtmlInlineKind.Text,      true,  "line b"),
			}));
	}
}
