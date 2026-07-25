using DailyReflection.Core.Extensions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using System.Collections.Generic;

namespace DailyReflection.Converters;

/// <summary>
/// Attached property that renders the DB's inline HTML (&lt;i&gt;…&lt;/i&gt;,
/// &lt;br/&gt;) into a TextBlock's Inlines via <see cref="HtmlInlineParser"/>.
/// Used from the FeedView value template in DailyReflectionPage.xaml, where
/// code-behind cannot reach the templated TextBlocks. Replaces the old
/// code-behind SetInlines plumbing (spec 003 §C).
/// </summary>
public static class HtmlEx
{
    public static readonly DependencyProperty InlinesSourceProperty =
        DependencyProperty.RegisterAttached(
            "InlinesSource",
            typeof(string),
            typeof(HtmlEx),
            new PropertyMetadata(null, OnInlinesSourceChanged));

    public static void SetInlinesSource(DependencyObject element, string? value)
        => element.SetValue(InlinesSourceProperty, value);

    public static string? GetInlinesSource(DependencyObject element)
        => (string?)element.GetValue(InlinesSourceProperty);

    private static void OnInlinesSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBlock target)
        {
            return;
        }

        target.Inlines.Clear();
        foreach (var inline in ParseInlines(e.NewValue as string))
        {
            target.Inlines.Add(inline);
        }
    }

    /// <summary>
    /// Materialises WinUI <see cref="Inline"/> elements from the platform-neutral
    /// <see cref="HtmlInlineParser"/> tokens. The parser itself is tested in
    /// <c>DailyReflection.Core</c>; this is purely a UI-side adapter.
    /// </summary>
    private static IEnumerable<Inline> ParseInlines(string? html)
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
