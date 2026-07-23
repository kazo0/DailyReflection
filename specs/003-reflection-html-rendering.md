f# Spec 003 — Reflection HTML rendering, ProgressRing z‑order, first‑load semantics

* **Status:** Implemented (2026-05-03).
* **Severity:** 🔴 Functional gap (10.1.1, 10.1.2) / 🟠 Behavioural drift (10.1.3, 10.1.4)
* **Gaps closed:** 10.1.1, 10.1.2, 10.1.3, 10.1.4
* **Depends on:** —

## Summary

The DailyReflection page on the Uno port has three regressions vs. the Xamarin original:

1. **HTML inline emphasis is lost.** The DB stores `Reading` (and sometimes other fields) wrapped in `<i>…</i>` and the copyright label as `From the book <i>Daily Reflections</i><br>Copyright …`. Xamarin renders these via `Label TextType="Html"`. Uno strips all tags through `HtmlToTextConverter` and applies a single page‑level italic on the reading block. Italics inside `Title` / `Thought` and the `<br>` in the copyright disappear.
2. **Loading spinner z‑order.** Xamarin's `ActivityIndicator` is the **last** child of the content `Grid` so it z‑orders above the partially‑rendered content. Uno's `ProgressRing` is declared *between* the StackPanel and a sibling Grid in the same parent; on first load it sits below the still‑empty StackPanel.
3. **First‑load semantics.** Xamarin invokes `vm.Init()` (an idempotent guard via `_initialized`) inside `OnAppearing`. Uno wires `Loaded += OnLoaded` and runs `GetDailyReflectionCommand` directly without idempotency, so re‑navigating to the tab re‑hits the database. `vm.Init()` is never called on the Uno port.

This spec restores all three behaviours.

## Goals

* Inline italic and line‑break markup from the DB renders correctly on the page.
* The loading indicator sits visually above content during fetches.
* The first navigation to the Reflection tab triggers `vm.Init()`; subsequent activations do not re‑fetch.

## Non‑goals

* Adding any HTML feature beyond `<i>` and `<br>` (those are the only tags the seeded DB uses — verify before closing this spec by grepping a fresh extract of `dailyreflections.db`).
* Replacing `HtmlToTextConverter` everywhere — it remains useful as a sanitiser for share‑text and the page Title binding (which renders in the `CommandBar.Content` and shouldn't carry markup).

## Acceptance criteria

1. A reflection whose `Reading` field is `<i>"Quote text"</i>` renders the entire reading in italic on the page.
2. A reflection whose `Title` or `Thought` contains an inline `<i>…</i>` segment renders that segment in italic with the surrounding text upright.
3. The copyright line renders as two visual lines: "From the book *Daily Reflections*" then "Copyright © 1990 by Alcoholics Anonymous World Services, Inc." with the title segment italicised.
4. While `GetDailyReflectionCommand.IsRunning` is true, the `ProgressRing` is visible and is rendered in front of any previously‑shown content (verifiable in the visual tree inspector).
5. `OnLoaded` calls `vm.Init()` (not the command directly). Navigating away from the Reflection tab and back does not cause `_dailyReflectionService.GetDailyReflection` to be called a second time on the same calendar day.
6. `vm.Init()` is exercised at least once per process; it remains idempotent.

## Implementation plan

### A. Inline‑markup parser

Add `Converters/HtmlToInlinesConverter.cs`. Rather than returning a string, it returns an `IEnumerable<Inline>` that XAML can plug into `RichTextBlock` or directly into `<TextBlock>` via `Inlines`.

```csharp
public sealed class HtmlToInlinesConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, string language)
    {
        if (value is not string s || string.IsNullOrEmpty(s))
            return Array.Empty<Inline>();

        return ParseInlines(s);
    }
    public object? ConvertBack(object? v, Type t, object? p, string l) => throw new NotImplementedException();

    private static IEnumerable<Inline> ParseInlines(string html)
    {
        // Tokenise on <i>, </i>, <br>. HtmlDecode each text run.
        // Yield Run/LineBreak/italic Run accordingly.
        // Treat <br/> and <br> identically.
    }
}
```

A small hand‑rolled tokenizer is sufficient — the universe of markup is `<i>`, `</i>`, `<br>`, `<br/>`, plus HTML entities. **Do not** pull in `HtmlAgilityPack` — overkill and adds 200 KB to the desktop binary.

Unit‑test the parser exhaustively in `DailyReflection.Presentation.Tests/Converters/HtmlToInlinesConverterTests.cs` (this is the first non‑VM test we add; add a minimal test class).

### B. Switch the converter on the page

`Views/DailyReflectionPage.xaml`:

```xml
<TextBlock Style="{StaticResource TitleTextStyle}"
           TextAlignment="Center">
    <TextBlock.Inlines>
        <Run Text="{x:Bind ViewModel.DailyReflection.Title, Mode=OneWay}" />
    </TextBlock.Inlines>
</TextBlock>
```

Hmm — `<Run Text>` does not parse markup. Instead use code‑behind binding: when `ViewModel.DailyReflection` changes, push the parsed inlines into the three relevant `TextBlock`s.

```csharp
ViewModel.PropertyChanged += (_, e) =>
{
    if (e.PropertyName == nameof(DailyReflectionViewModel.DailyReflection))
        RefreshInlines();
};

private void RefreshInlines()
{
    var r = ViewModel.DailyReflection;
    if (r is null) return;
    SetInlines(TitleText,   r.Title);
    SetInlines(ReadingText, r.Reading);
    SetInlines(ThoughtText, r.Thought);
}
private static void SetInlines(TextBlock tb, string? html)
{
    tb.Inlines.Clear();
    if (string.IsNullOrEmpty(html)) return;
    foreach (var inline in HtmlToInlinesConverter.ParseInlines(html))
        tb.Inlines.Add(inline);
}
```

Name the three `TextBlock`s in XAML (`x:Name="TitleText"` etc.) and remove their existing `Text="{x:Bind ... Converter=HtmlToTextConverter}"` bindings.

For the **copyright line**, hard‑code the `Run`/`LineBreak` structure in XAML — it does not come from the DB:

```xml
<TextBlock Style="{StaticResource CaptionTextStyle}" Margin="0,20,0,0">
    <Run Text="From the book " />
    <Run FontStyle="Italic" Text="Daily Reflections" />
    <LineBreak/>
    <Run Text="Copyright © 1990 by Alcoholics Anonymous World Services, Inc." />
</TextBlock>
```

(The current XAML already does this — verify it survives the cleanup.)

### C. ProgressRing z‑order

Re‑order the children of the inner `Grid` in `DailyReflectionPage.xaml` so `ProgressRing` is **last**:

```xml
<ScrollViewer Grid.Row="1">
    <Grid>
        <StackPanel ...>...</StackPanel>
        <Grid Visibility="...HasError...">
            <TextBlock Text="An error occurred. Please try again." ... />
        </Grid>
        <ProgressRing IsActive="..." HorizontalAlignment="Center" VerticalAlignment="Center" />
    </Grid>
</ScrollViewer>
```

Last child = top z‑order in WinUI/Uno. Same pattern as the Xamarin original.

### D. First‑load via `vm.Init()`

In `Views/DailyReflectionPage.xaml.cs`:

```csharp
private async void OnLoaded(object sender, RoutedEventArgs e)
{
    await ViewModel.Init();
}
```

`Init()` already guards via `_initialized` in `DailyReflectionViewModel`. Remove the direct `GetDailyReflectionCommand.ExecuteAsync(null)` call.

Confirm that `Init()` runs on the UI thread — `Init()` itself awaits the DB call, which marshals back to the captured context. WinUI/Uno page `Loaded` callbacks are already on the UI thread, so this is fine.

### E. Tests

Add to `DailyReflection.Presentation.Tests`:

* `HtmlToInlinesConverterTests` covering: plain text, `<i>` only, `<br>` only, mixed, malformed (missing close tag → degrade to plain text), HTML entities (`&amp;`, `&copy;`).
* A regression test for `DailyReflectionViewModel.Init()` confirming it is idempotent (call twice, assert `_dailyReflectionService.GetDailyReflection` invoked once).

## Risks & open questions

1. **DB content drift.** If a reflection ever gets `<b>` or `<em>` markup added in a future DB refresh, the parser would need extension. Out of scope today; document in code that the parser is intentionally minimal.
2. **`RichTextBlock` vs. `TextBlock.Inlines`.** Both can render `Run`/`LineBreak`. `TextBlock.Inlines` keeps the existing `Style` resources. Prefer it.
3. **Performance.** Parsing on every property change is fine — the strings are short (< 1 KB). No need to memoise.

## Done when

- [x] `HtmlInlineParser` added under `DailyReflection.Core/Extensions/` (platform-neutral) and `HtmlToInlinesConverter` in the Uno head wraps it for WinUI `Inline` materialisation.
- [x] `DailyReflectionPage.xaml.cs` subscribes to the VM's `PropertyChanged` and refreshes inlines when `DailyReflection` changes; `Unloaded` removes the subscription.
- [x] `ProgressRing` is the last child of its `Grid` (z-order matches the Xamarin original).
- [x] `OnLoaded` calls `ViewModel.Init()`; idempotency covered by a regression test.
- [x] 10 parser tests + `Init_Is_Idempotent` test green.
- [x] Uno desktop builds clean.

### Implementation deviations from the original plan

* **Parser placement.** The plan put the parser in the Uno head's `Converters/HtmlToInlinesConverter.cs`. It now lives in `DailyReflection.Core/Extensions/HtmlInlineParser.cs` returning a platform-neutral `HtmlInline` token type. The Uno converter is a thin adapter that materialises WinUI `Run` / `LineBreak` from the tokens. Lets unit tests run without WinUI.
* **Lifecycle hooks.** Spec wrote the property-change subscription as a one-line lambda inline in the constructor; the implementation hoists it to a named `OnViewModelPropertyChanged` so the `Unloaded` handler can detach it cleanly without leaking subscriptions on re-navigation (matches the lifecycle discipline spec 006 will formalise).
