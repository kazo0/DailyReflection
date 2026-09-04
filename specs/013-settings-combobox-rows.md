# Spec 013 — Settings enum pickers as card‑look ComboBoxes

* **Status:** Implemented (2026-08-28). Row rendering and dropdown placement verified on Skia desktop; the final build's dropdown item styling and an end‑to‑end selection through the new control still need a manual look (see Risks §4).
* **Severity:** 🟡 UX / parity *(replaces an interaction pattern; no §10 gap)*
* **Gaps closed:** —
* **Depends on:** [012](012-app-theme-preference.md) (introduces the second picker), [004](004-theme-and-shell-chrome.md) (palette / card look)

## Summary

The Settings page had two enum‑backed choices — **Sober Time Display** and (from spec 012) **Theme** — implemented as a tappable `FilledCardContentControl` row plus a *hidden* `ComboBox` (`Visibility="Collapsed"`) that the tap handler made visible and opened, then collapsed again on selection. On desktop the first tap often only revealed the ComboBox without opening its list, and either way a second, differently‑styled control appeared under the row.

The owner's direction: *there shouldn't be a hidden ComboBox — the row should be a styled ComboBox that matches the row items.* This spec makes each of those rows a real `ComboBox` whose template **is** the card row: same surface, corners, padding, title / value typography, chevron and state overlays as the neighbouring `FilledCardContentControl` rows; tapping anywhere on it opens a Material‑styled list directly beneath.

## Goals

* One control per row; no hidden or duplicated controls in `SettingsPage.xaml`.
* Visually indistinguishable from the other Settings cards at rest (title in BodyMedium, current value in BodySmall / secondary, right chevron, 12 px corners, 16,12 padding, hover / pressed / focus overlays of the filled card).
* Dropdown opens under the row (not over it), with Material list rows and the current value highlighted; both light and dark themes.
* The page XAML for a row is a single self‑describing element; templates and text formatting live in the style.
* Automation ids and the TwoWay `SelectedItem` → MVUX state contract are unchanged.

## Non‑goals

* Restyling any other Settings row (date / time pickers keep their flyouts; toggles unchanged).
* Basing the style on `MaterialComboBoxStyle` — its outlined text‑field look with a floating label is a different shape, and `BasedOn` across App.xaml merged dictionaries is the unreliable StaticResource case (see `PickerFlyouts.xaml`).

## Acceptance criteria

1. `SettingsPage.xaml` contains exactly one `<ComboBox … />` per enum setting (`settings_sober_time_display`, `settings_theme_pref`), each with `Style="{StaticResource DRSettingsComboBoxStyle}"`, a `Header`, `ItemsSource="{Binding All…}"` and `SelectedItem="{Binding …, Mode=TwoWay}"`; no `ComboBox` in the page has `Visibility="Collapsed"` (lint test).
2. No `*_Tapped` / `*_SelectionChanged` code‑behind exists for those rows.
3. At rest the row renders: title = `Header` (BodyMedium, `OnSurface`), value = display text of `SelectedItem` (BodySmall, `TextFillColorSecondaryBrush`), chevron `E76C`, surface background, 12 px corners — matching the adjacent cards in both themes.
4. Tapping anywhere on the row opens the list below it; the list shows the display strings (`Days, Months, and Years` / `Days Only`; `System` / `Light` / `Dark`) with the current one highlighted; choosing one closes the list, updates the row value and writes the MVUX state (persist + side effects).
5. Display strings are the Xamarin app's, produced by one converter for both enums.

## Implementation plan

### A. `Styles/SettingsComboBox.xaml` (merged in `App.xaml`)

**`DRSettingsComboBoxStyle`** (`TargetType="ComboBox"`):

* Setters: `Background=SurfaceBrush`, `Foreground=OnSurfaceBrush`, `CornerRadius=12`, `Padding=16,12`, stretch alignment, `TabNavigation=Once`, the usual `ScrollViewer.*` setters, `ItemContainerStyle=DRSettingsComboBoxItemStyle`, an `ItemTemplate` (`TextBlock` bound through `SettingsChoiceConverter`), and `uno:ComboBox.DropDownPreferredPlacement="Below"` (`xmlns:uno="using:Uno.UI.Xaml.Controls"`; all heads are Uno so the attached property needs no conditional prefix).
* Template — a `Grid` with:
  * `CommonStates` Normal / PointerOver / Pressed / Disabled and `FocusStates`, driving three overlay `Border`s (`HoverOverlay` = `OnSurfaceHoverBrush`, `PressedOverlay` = `OnSurfacePressedBrush`, `FocusedOverlay` = `OnSurfaceFocusedBrush`) exactly like Uno Toolkit's `MaterialFilledCardContentControlStyle`; `DropDownStates` Opened / Closed present but empty.
  * The card: `Grid x:Name="Card"` (background + corner radius) → padded `Grid` (`*`, `Auto`) → `StackPanel` with `HeaderText` (`Text="{TemplateBinding Header}"`, BodyMedium typography via the `BodyMedium*` ThemeResource keys) and `ValueText` (`Text="{Binding SelectedItem, RelativeSource={RelativeSource TemplatedParent}, Converter={StaticResource SettingsChoiceConverter}}"`, BodySmall keys, secondary foreground); `FontIcon` chevron in column 1.
  * `ContentPresenter x:Name="ContentPresenter"` kept **collapsed** inside the StackPanel. Uno's `ComboBox` resolves this part by name and writes the selection into it through `ItemTemplate`; it is not used for display because a `TextBlock` produced by `ItemTemplate` picks up Material's implicit `TextBlock` style (`MaterialDefaultTextBlockStyle` = BodyMedium, FontSize 14) and cannot be shrunk to the row's BodySmall from the presenter. `ValueText` renders the value instead.
  * `Popup x:Name="Popup"` → `Border x:Name="PopupBorder"` (surface, 1 px `OnSurfaceLow` border, same corner radius, 4 px top margin) → `ScrollViewer x:Name="ScrollViewer"` → `ItemsPresenter`. These are the template parts Uno's `ComboBox` requires; Uno sizes `PopupBorder` to the control's width.

**`DRSettingsComboBoxItemStyle`** (`TargetType="ComboBoxItem"`): 48 px min height, 16,0 padding, `OnSurface` foreground; one `StateLayer` border — `PrimarySelectedBrush` for Selected / SelectedPointerOver / SelectedPressed / SelectedUnfocused, `OnSurfaceHoverBrush` / `OnSurfacePressedBrush` for PointerOver / Pressed, 0.38 opacity for Disabled. Without an explicit item style the dropdown fell back to Fluent's compact items.

**Resource rules.** The converter instance and both styles are defined in this one dictionary because same‑file `StaticResource` lookups are the reliable kind; every colour / typography value is a `ThemeResource` palette or typography key (all 15 confirmed present in Uno.Material 7.2), never a Material *style* key.

### B. `Converters/SettingsChoiceConverter.cs`

One `IValueConverter` for both enums (`SoberTimeDisplayPreference` → "Days, Months, and Years" / "Days Only"; `AppThemePreference` → "System" / "Light" / "Dark"; anything else → `ToString()`). Replaces `SoberTimeDisplayEnumConverter` and the short‑lived `AppThemePreferenceEnumConverter`; the `App.xaml` registrations of those two are removed.

### C. `Views/SettingsPage.xaml` / `.xaml.cs`

Each row becomes:

```xml
<ComboBox Style="{StaticResource DRSettingsComboBoxStyle}"
          Header="Theme"
          ItemsSource="{Binding AllAppThemePreferences}"
          SelectedItem="{Binding AppThemePreference, Mode=TwoWay}"
          AutomationProperties.AutomationId="settings_theme_pref" />
```

The card + hidden‑ComboBox blocks and the four handlers (`SoberTimeDisplay_Tapped`, `SoberTimeDisplayComboBox_SelectionChanged`, `AppThemePreference_Tapped`, `AppThemePreferenceComboBox_SelectionChanged`) are deleted.

### D. Tests

`ViewSurfaceTests.SettingsPage_enum_rows_are_visible_styled_ComboBoxes` — for each of the two automation ids: the element is a `ComboBox`, uses `DRSettingsComboBoxStyle`, has the expected `Header`, `ItemsSource` and TwoWay `SelectedItem`; and the page contains no `ComboBox` with `Visibility="Collapsed"`.

## Risks & open questions

1. **Uno template‑part contract.** The template keeps `Popup`, `PopupBorder`, `ScrollViewer`, `ContentPresenter`. If a future Uno adds a required part, the control degrades rather than crashes, but re‑check after Uno upgrades.
2. **Header binding.** `Text="{TemplateBinding Header}"` assumes a string `Header`; a non‑string header would need `HeaderTemplate` support in the template.
3. **Popup elevation.** Uno Skia popups have no shadow; the 1 px `OnSurfaceLow` border stands in for it (same choice as the picker flyouts).
4. **Manual verification gap.** The desktop check confirmed the rows' rendering, that the list opens under the row with the current item highlighted, and Fluent‑styled items *before* `DRSettingsComboBoxItemStyle` was added; the final build (Material items) compiled and its resource keys were verified statically, but its dropdown and a selection through it were not exercised interactively. Do that once on desktop in both themes.
5. **Mobile.** Android compiles; runtime behaviour of the dropdown on Android / iOS (native popup placement) untested.

## Done when

- [x] `Styles/SettingsComboBox.xaml` with `DRSettingsComboBoxStyle` + `DRSettingsComboBoxItemStyle` + the converter instance; merged in `App.xaml`.
- [x] `SettingsChoiceConverter` replaces the two per‑enum converters.
- [x] Sober Time Display and Theme rows are single `ComboBox` elements; hidden ComboBoxes and their handlers removed.
- [x] Automation ids preserved on the ComboBoxes; coverage lint green.
- [x] `ViewSurfaceTests` asserts the ComboBox contract and the absence of hidden ComboBoxes.
- [x] Desktop + Android heads compile; unit suites green.
- [x] Desktop screenshot: rows match the neighbouring cards in light theme; list opens under the row.
- [ ] Manual: open the list in the final build (light + dark), confirm Material item rows and that a pick updates the row and persists.
- [ ] Manual: Android / iOS dropdown smoke test.
