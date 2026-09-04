# Spec 014 — Reflection paging: FlipView + PipsPager over every reading

* **Status:** In progress — **draft reconstructed from the uncommitted working‑tree diff on `feat/flipview-pips-pager` (2026-08-28)**. The checklist below reflects what the diff already contains; unticked items are what remains to confirm.
* **Severity:** 🟢 New feature *(the Xamarin app showed one day at a time and moved only through the date picker)*
* **Gaps closed:** —
* **Depends on:** [003](003-reflection-html-rendering.md) (HTML inline rendering / FeedView templates), [005](005-automation-ids.md)

## Summary

The Reflection tab currently loads one reading for the selected `Date`. This spec turns the page into a **swipeable book**: every reading (Jan 1 → Dec 31, Feb 29 included) is loaded once into a list feed and presented in a `FlipView`, one reading per page, with a Toolkit‑synced `PipsPager` (previous / next arrows) beneath it. Swiping, the arrows and the date picker all move the same thing — the model's `Date` — so the reading shown, the reading shared, and the FlipView position never disagree.

Design decisions captured from the diff:

* **`Date` stays the single source of truth.** `DailyReflection` (the reading shown / shared) and `SelectedIndex` (the FlipView position) are both projections of `Date` over the loaded list. Paging writes `SelectedIndex`, which is mapped back onto `Date`; picking a date moves the FlipView the other way round.
* **The FlipView is synced in code‑behind, not by binding.** MVUX's `Selection()` operator only bridges `ListViewBase`, and a TwoWay binding on `FlipView.SelectedIndex` is unsafe: the FlipView auto‑selects item 0 the moment its `ItemsSource` lands, which a binding would push into the model as "the user paged to January 1st". The page positions the FlipView from the model first and only then subscribes to `SelectionChanged` / `PropertyChanged`.
* **Pages are readings, not calendar dates.** A page maps to its month/day in the current year — except February 29, which maps to the most recent leap year so the reading stays reachable by swiping (the date picker can't select it in other years). Only month and day are ever displayed or shared.
* **Programmatic moves jump.** A model‑driven move can span hundreds of pages (Jan 1 → today at startup, or a date pick); `UseTouchAnimationsForAllNavigation` is switched off around it so the FlipView doesn't animate through every page.

## Goals

* Swipe (touch) or use the pager arrows / keyboard to move to the previous or next day's reading; the page date in the NavigationBar and the Share payload follow.
* Picking a date in the flyout jumps the FlipView to that reading.
* One database read for the whole list per model lifetime (the model is a singleton); no per‑page reloads.
* Loading, empty and error states keep the spec 003 FeedView templates.
* Automation ids for the FlipView and pager (`reflection_flipview`, `reflection_pager`); the per‑reading ids (`reflection_title`, `reflection_quote`, …) move into the page template unchanged.

## Non‑goals

* Infinite / circular paging across the year boundary.
* Persisting the last viewed page; the tab opens on today.
* Changing the reading layout itself (title / quote / source / thought / copyright and their HTML rendering are as in spec 003).

## Acceptance criteria

1. `IDailyReflectionDatabase.GetAllReflections()` / `IDailyReflectionService.GetAllReflections()` return every row ordered by month then day; `DailyReflectionModel.Reflections` (an `IListFeed<Reflection>`) loads it exactly once per model instance, shared by every consumer of the feed.
2. `DailyReflectionModel.DailyReflection` yields the reading whose month/day match `Date`, or None when there is no such reading (error template).
3. `DailyReflectionModel.SelectedIndex` equals the index of `Date`'s reading in `Reflections` (‑1 when absent) and follows `Date` when it changes.
4. Setting `SelectedIndex` to a valid index whose reading differs from `Date` sets `Date` to that reading's month/day in the current year — or in the most recent leap year for Feb 29 — and `DailyReflection` follows.
5. Setting `SelectedIndex` to the index of the reading already shown, or to an out‑of‑range value, leaves `Date` untouched (this is what protects a picked year from being rewritten by the view echoing the index back).
6. In the view: the FlipView's initial auto‑selection never reaches the model; a swipe / arrow / pip writes the model's `SelectedIndex`; a model change (date pick, or the initial index arriving after load) moves the FlipView without animating through intermediate pages.
7. `DailyReflectionPage.xaml` declares the FlipView with `ItemsSource="{Binding Data}"`, synced to the named `PipsPager` through `utu:SelectorExtensions.PipsPager`, and sets neither `NumberOfPages` / `SelectedPageIndex` on the pager nor a `SelectedIndex` / `SelectedItem` binding on the FlipView (lint test).
8. Startup opens on today's reading; Share still shares the reading being viewed (`"Daily Reflection {date:MMM d}"` + reflection text).

## Implementation plan

### A. Data / Services

* `DailyReflection.Data/Databases/DailyReflectionDatabase.cs` — `Task<List<Reflection>> GetAllReflections()` on the interface and implementation: `_db.Table<Reflection>().OrderBy(d => d.Month).ThenBy(d => d.Day).ToListAsync()`.
* `DailyReflection.Services/DailyReflection/DailyReflectionService.cs` — pass‑through `GetAllReflections()` on `IDailyReflectionService` (kept alongside `GetDailyReflection` for now).

### B. Presentation — `DailyReflectionModel`

```csharp
Reflections = ListFeed.Async(async ct =>
	(IImmutableList<Reflection>)(await service.GetAllReflections()).ToImmutableList());
_reflections = Reflections.AsFeed();

DailyReflection = Feed.Combine(_reflections, Date)
	.Select(x => FindReflection(x.Item1, x.Item2))
	.Where(reflection => reflection is not null);

SelectedIndex = State
	.FromFeed(this, Feed.Combine(_reflections, Date).Select(x => IndexOf(x.Item1, x.Item2)))
	.ForEach(OnSelectedIndexChanged);
```

`OnSelectedIndexChanged` ignores negative / out‑of‑range indices and indices whose reading already matches `Date` (the state's own replay or a view echo), otherwise `Date.SetAsync(ToDate(reflection))`. `ToDate` walks the year back while the day doesn't exist in the month (Feb 29 → last leap year).

Feeds are cached per `SourceContext`; the bindable gives the model one context in the app, so every consumer shares the single `GetAllReflections` call (the tests await under that context to mirror it).

### C. Head — `DailyReflectionPage.xaml`

* `FeedView Source="{Binding Reflections}"` (no outer `ScrollViewer` any more — each FlipView page scrolls itself).
* Value template: `Grid` (`*`, `Auto`) → `FlipView` (`ItemsSource="{Binding Data}"`, `Background="Transparent"`, `utu:SelectorExtensions.PipsPager="{Binding ElementName=ReflectionPager}"`, `Loaded` / `Unloaded` handlers, automation id `reflection_flipview`) whose `ItemTemplate` is a `ScrollViewer` around the spec 003 reading `StackPanel` (bindings drop the `Data.` prefix); `PipsPager x:Name="ReflectionPager"` (`MaxVisiblePips="5"`, previous / next buttons visible, automation id `reflection_pager`) in row 1.
* Progress / None / Error templates unchanged (None now means "no readings loaded").
* Incidental: the `DatePickerFlyout`'s `YearVisible="False"` attribute is terminated (the `master` version was missing the closing quote, which broke the head build).

### D. Head — `DailyReflectionPage.xaml.cs`

* `ReflectionsFlipView_Loaded`: keep the FlipView, `ApplySelectedIndex()` **first**, then subscribe `flipView.SelectionChanged` and `viewModel.PropertyChanged`.
* `ReflectionsFlipView_Unloaded`: unsubscribe both, drop the reference.
* `ReflectionsFlipView_SelectionChanged`: if `SelectedIndex >= 0` and differs from the model's, write `viewModel.SelectedIndex`.
* `ViewModel_PropertyChanged` (`SelectedIndex`): `ApplySelectedIndex()`.
* `ApplySelectedIndex`: bail on missing FlipView / model, out‑of‑range or equal index; otherwise set `flipView.SelectedIndex` with `UseTouchAnimationsForAllNavigation` temporarily `false`.
* `DatePickerFlyout_DatePicked` unchanged in effect (writes `viewModel.Date`).

### E. Constants / tests

* `AutomationConstants.DR_Reflection_FlipView = "reflection_flipview"`, `DR_Reflection_Pager = "reflection_pager"`.
* `DailyReflectionModelTests` — `Reflections_Loads_Every_Reading_Once`, `SelectedIndex_Follows_Date`, `Setting_SelectedIndex_Updates_Date_To_That_Reading`, `Setting_SelectedIndex_To_Feb_29_Yields_A_Leap_Year_Date`, `Setting_SelectedIndex_To_Current_Reading_Leaves_Date_Alone`, `Setting_SelectedIndex_Out_Of_Range_Leaves_Date_Alone` (fixture: Jan 1, Feb 29, today, Dec 31 in calendar order; `GetAllReflections` mocked).
* `DailyReflectionServiceTests.GetAllReflections_Calls_Database`.
* `ViewSurfaceTests.DailyReflectionPage_pages_readings_with_FlipView_synced_to_PipsPager` — asserts the XAML shape in criterion 7 — and the automation‑id assertions extended with the two new ids.

## Risks & open questions

1. **Memory / first paint.** 366 readings materialise as one immutable list (small — text only), but the FlipView virtualises its pages; confirm on Android that the initial jump from item 0 to today doesn't flash January 1st. If it does, consider hiding the FlipView until the first `ApplySelectedIndex` has run.
2. **`SelectedIndex` echo protection relies on month/day equality**, so a year picked in the flyout survives a paging round trip only because the same reading is matched; paging *away* and back lands on the current year (or the last leap year). That is the documented "pages are readings, not dates" behaviour — check it reads as intended.
3. **Keyboard / pager buttons on desktop** route through `FlipView.SelectionChanged` like swipes; verify the pager's previous/next buttons on Skia (Toolkit `SelectorExtensions`).
4. **`IDailyReflectionService.GetDailyReflection` is now unused by the model** — keep for other callers or remove in a follow‑up.
5. **Feb 29 outside leap years** is reachable only by swiping; the picker can't select it. Acceptable by design; worth a line in the README if users ask.

## Done when

- [x] `GetAllReflections()` on database and service, with service test.
- [x] `DailyReflectionModel`: `Reflections` list feed, `DailyReflection` and `SelectedIndex` as projections of `Date`, index → date mapping with the Feb 29 rule.
- [x] `DailyReflectionPage`: FlipView + PipsPager in the FeedView value template; code‑behind sync with load‑then‑subscribe ordering and animation‑free programmatic jumps.
- [x] Automation ids added and covered; view‑surface lint for the FlipView / pager contract.
- [x] Presentation + service tests for the model / service behaviour above.
- [x] Desktop head builds; unit suites green (31 service + 35 presentation on 2026-08-28, together with specs 012–013).
- [ ] Desktop runtime: swipe / arrows / date pick / share all agree; startup lands on today without a visible jump.
- [ ] Android / iOS runtime: touch paging, pager buttons, orientation change.
- [ ] Decide the fate of `GetDailyReflection` (risk 4) and note the Feb 29 rule in `README.md` if desired.
