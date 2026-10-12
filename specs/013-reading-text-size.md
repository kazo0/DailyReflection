# Spec 013 — Reading text size

* **Status:** Implemented (2026-10-12).
* **Severity:** 🔴 (store reviews; issues [#69](https://github.com/kazo0/DailyReflection/issues/69), [#70](https://github.com/kazo0/DailyReflection/issues/70), [#71](https://github.com/kazo0/DailyReflection/issues/71))
* **Depends on:** [006](006-settings-correctness.md) (settings rows), [003](003-reflection-html-rendering.md)

## Summary

Since the Uno port shipped (4.0.x), the reading on the Reflection tab is set in the Material theme's `BodyMedium` (14) with a `HeadlineSmall` title (24). The Xamarin app used the platform's named sizes — on iOS `Body` 17 and `Title` 28 — so the body text is 18% smaller than before and every store review and issue since has been about it ("print size is very small", "no adjustment to the text size", on iPad "shrinks to a small area center screen", the latter because #27 also capped the column at 720px). Reproduced in the iOS 27 simulator on 2026-10-12: at the default text size the reading is 14pt; on an iPad Pro 11" it is a small block at the top of a mostly empty screen.

The OS text size setting *does* reach the app on iOS (verified at the largest accessibility size: the reading scales, Uno's Skia host applies it), but only after a relaunch, and at those sizes the tab labels overflow their third of the bar and run into each other ("ReflecticSober TiSettings").

The other tabs shrank the same way (compared against the 2020 Xamarin screenshots in git history, `images/ios/iPhone65`): the Sober Time count and date went from `Title` 28 to `HeadlineSmall` 24 and the "One day at a time" tagline from `Subtitle` 22 to the unstyled 14; the Settings rows from the iOS table cells' 17 to `BodyMedium` 14 with `BodySmall` 12 details. The navigation bar title (17 → 22) and the tab labels (10 → 12) grew, and stay as they are.

This spec restores the Xamarin sizes on every tab, adds an in-app Text Size setting on top of them, scales the column with the text, and keeps the tab labels fixed.

## Goals

* The reading's default sizes are the Xamarin app's: title 28, body (quote, source, thought) 17, copyright 13.
* Settings → Readings offers **Text Size**: Normal, Large, Extra Large, Huge. The choice persists and applies to the Reflection and Sober Time tabs at once.
* The reading column keeps the same line length at every size (the 720px cap scales with the text).
* Sober Time's date and count are 28 and its tagline 22 italic, scaled by the same setting; the Settings row labels are 17 with 15 for the value / description line.
* Tab labels stay at the theme's label size whatever the OS text size.

## Non‑goals

* Scaling the Settings tab from the in-app setting (the OS text size already applies to it), or making anything smaller than 4.0.39 has it: the 22 section headers, the navigation bar title and the tab labels keep their sizes.
* A size below Normal — every report asked for larger.
* Re-rendering live when the OS text size changes while the app is open (framework behaviour; a relaunch applies it).

## Design

### A. Setting (`SettingsModel`, `SettingsPage.xaml`)

* `ReadingTextSize` enum in `DailyReflection.Core/Entities`: `Normal = 0, Large, ExtraLarge, Huge`. Stored as its integer under `PreferenceConstants.ReadingTextSize`; `Normal` must stay 0 so fresh and existing installs read back as it. Declaration order is the picker order.
* `ReadingTextSizeExtensions.ToScale()` maps a size to the factor the page uses: 1 / 1.2 / 1.4 / 1.65. Anything outside the enum degrades to 1.
* `SettingsModel.ReadingTextSize` (`IState<ReadingTextSize>`) mirrors `SoberTimeDisplayPreference`: seeded from the store, compare-before-persist in its `ForEach` so startup is side-effect free, then `Set` and an `EntityMessage<ReadingTextSizeSelection>` through the singleton `IMessenger`. `AllReadingTextSizes` feeds the picker.
* `SettingsPage.xaml`: a `ComboBox` in the Readings section, `DRSettingsComboBoxStyle`, header "Text Size", labelled through `ReadingTextSizeEnumConverter`, automation id `settings_reading_text_size`.

### B. Reflection tab (`DailyReflectionModel`, `DailyReflectionPage.xaml`)

* The model observes the message on a `ReadingTextSizeSelection` state seeded from the store and projects it into `IFeed<double> TextScale`.
* The page's six reading `TextBlock`s keep their Material styles for family and weight but bind `FontSize` to `Parent.TextScale` through `ScaleConverter` with the base size as `ConverterParameter` (28 / 17 / 17 / 17 / 13 / 13); the `StackPanel`'s `MaxWidth` binds the same way from 720. `Parent` because the `FeedView` templates' DataContext is the feed state. The converter returns the base value until the scale is bound, so the page never flashes at the theme's size.
* The OS text size is applied by the framework on top of this (iOS and Android); on desktop and web the in-app setting is the only scaling.

### C. Tab labels (`MainPage.xaml`)

* Each of the six `TabBarItem`s gets an explicit `TextBlock` as `Content` with `IsTextScaleFactorEnabled="False"`, matching native iOS tab bars. Not a `ContentTemplate`: Uno's `ContentPresenter` also binds the templated parent's `ContentTemplate` for the Icon presenter, which then rendered the label template instead of the icon (reproduced in the simulator).

### D. Sober Time and Settings (`SobrietyTimeModel`, `SobrietyTimePage.xaml`, `SettingsPage.xaml`)

* `SobrietyTimeModel` observes the same message on its own `ReadingTextSizeSelection` state and projects `IFeed<double> TextScale`, exactly as §B. The page binds `FontSize` straight to `TextScale` (no `FeedView`, so no `Parent.`) through `ScaleConverter`: the calendar icon, the date and the eight count blocks from 28, the tagline from 22. The converter treats a non-positive scale as unbound, because the generated view model exposes the feed as `0` until its first value and these `TextBlock`s render before that.
* The picker sits under the Readings header and now drives Sober Time too; a rename or move is a product call, not part of this spec.
* `SettingsPage.xaml` overrides `BodyMediumFontSize` (17) and `BodySmallFontSize` (15) in `Page.Resources` — Uno Themes' lightweight styling — so the row labels, subtitles and the `SettingsRows.xaml` templates (`{ThemeResource …}` header presenters and value lines) take the old sizes while `MainPage`'s tab labels and the other pages keep the theme's. Not an app-wide override, for that reason. Section headers stay `TitleLarge`.

## Acceptance criteria

1. Fresh install, default OS text size: the reading renders at 17pt body / 28pt title (iOS simulator screenshot compared against 4.0.39).
2. Settings → Readings → Text Size lists Normal, Large, Extra Large, Huge; choosing one rescales the Reflection and Sober Time tabs without a relaunch and survives a relaunch.
3. Nothing is persisted or sent on startup (`Load_Does_Not_Persist_Or_Reschedule`).
4. With the OS text size at its largest accessibility setting, the three tab labels stay readable and apart, with their icons.
5. Sober Time at Normal: 28 count / date, 22 italic tagline. Settings at Normal: 17 row labels, 15 details; the headers, navigation bar and tab labels unchanged from 4.0.39.
6. `ViewSurfaceTests` pins the six scaled reading `FontSize` bindings, the scaled `MaxWidth`, the eleven scaled Sober Time bindings, the two Settings token overrides (and their absence from `App.xaml`), the Settings picker binding and the six fixed tab labels; `AutomationConstantsCoverageTests` sees the new id.

## Risks

* **Desktop compositor bug** ([unoplatform/uno#24768](https://github.com/unoplatform/uno/issues/24768), see AGENTS.md): changing the size in Settings and returning to Reflection can show overlapping text until the window is resized — the same stale-rendering issue as the secular toggle, not a regression of this spec.
* `Huge` at the largest OS accessibility size is very large (17 × 1.65 × ~3.1 ≈ 87pt); the page scrolls, and anyone who chose both wanted it.

## Done when

- [x] `ReadingTextSize`, `ReadingTextSizeSelection`, `PreferenceConstants.ReadingTextSize` in Core.
- [x] `SettingsModel.ReadingTextSize` persists, sends, and is side-effect free on load.
- [x] `DailyReflectionModel.TextScale` and `SobrietyTimeModel.TextScale` follow the setting.
- [x] `DailyReflectionPage.xaml` and `SobrietyTimePage.xaml` size from the Xamarin bases through `ScaleConverter`; the reading column scales.
- [x] `SettingsPage.xaml` Text Size picker with its automation id; rows at 17 / 15 through the page-scoped token override.
- [x] `MainPage.xaml` tab labels fixed; icons intact.
- [x] Tests: `SettingsModelTests`, `DailyReflectionModelTests`, `SobrietyTimeModelTests`, `ViewSurfaceTests`.
- [x] `release_notes.txt` updated for the next release.
