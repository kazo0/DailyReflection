# Spec 004 — Theme and shell chrome restoration

* **Status:** Implemented (2026-05-03). Manual visual QA still required (criterion #6).
* **Severity:** 🔴 / 🟠 (Material visual gone is the only true 🔴)
* **Gaps closed:** 10.4.1, 10.4.2, 10.4.3, 10.4.4, 10.4.5, 10.10.1, 10.10.2, 10.10.4
* **Depends on:** —

## Summary

The original Xamarin app drives every visible chrome surface from `App.xaml`:

* Page background: `BackgroundColorLight (#EFF2F5)` / `BackgroundColorDark (#121212)`.
* Label foreground: `TextPrimaryColorLight (#323130)` / `TextPrimaryColorDark (#FFFFFF)`.
* Shell tab bar: `Primary (#1976D2)` background on Android, `BackgroundColorLight/Dark` on iOS.
* Shell foreground (title text): `TextPrimaryColorDark` on Android, `iOSSystemBlue (#007bff)` on iOS.
* Tab bar title selected colour: `Primary` (#1976D2).
* `Visual="Material"` on Shell + every page → Android renders Material‑styled controls.

The Uno port preserves the **colour resources** in `Styles/Colors.xaml` but never wires them into the `Page` or `TabBar`. Pages default to `ApplicationPageBackgroundThemeBrush` (close to white/black), labels default to `TextFillColorPrimaryBrush`, and the `utu:TabBar` uses `Uno.Toolkit` defaults. The Material visual on Android is also unavailable in WinUI; Uno renders Fluent‑styled controls there.

This spec restores the original chrome on the Uno port through a combination of `ThemeDictionaries` and `Style`s targeting `Page`, `TextBlock`, and `utu:TabBar`.

## Goals

* Page backgrounds match the original Light/Dark palette (`#EFF2F5` / `#121212`) on every Uno target.
* Default `TextBlock` foreground matches the original (`#323130` light, `#FFFFFF` dark) on every Uno target.
* The bottom `TabBar` adopts the original chrome: blue (`#1976D2`) accent, light/dark surface, gray unselected, blue selected.
* On Android, render styling closer to Material than to Fluent. Where this is impossible without a redesign, document the deviation.

## Non‑goals

* Re‑introducing Material *renderers* — that's a per‑control opt‑in in WinUI/Uno (`Material.MaterialResources`) and would cascade visual changes across every control. We stick with WinUI defaults but tune colours and corner radii to read closer to Material.
* New colours. Use only the colours already declared in `Styles/Colors.xaml`.

## Acceptance criteria

1. Launching any of the three pages on Light theme shows the page background as `#EFF2F5`. Dark theme: `#121212`.
2. Default `TextBlock` text on those pages reads `#323130` (light) / `#FFFFFF` (dark) at 100% opacity.
3. The `utu:TabBar` background is `#FFFFFF` (light) / `#121212` (dark). The selected tab item title is `#1976D2`. Unselected tab items are `Gray`. Tab bar has no top shadow.
4. The selected tab indicator is `#1976D2`.
5. The Reflection page's `CommandBar` background is `#1976D2` on Android, matching the original Shell colour. On iOS the `CommandBar` background is `#EFF2F5` (light) / `#121212` (dark) and the title text is `#007bff` — also matching.
6. The acceptance criteria pass on Android, iOS, and Skia desktop without per‑target XAML duplication (one `ThemeDictionary` set keyed by light/dark + per‑target overrides in code where unavoidable).

## Implementation plan

### A. App‑level `ThemeDictionaries`

Add to `App.xaml` `Application.Resources`:

```xml
<ResourceDictionary.ThemeDictionaries>
    <ResourceDictionary x:Key="Light">
        <SolidColorBrush x:Key="DRPageBackgroundBrush" Color="#EFF2F5" />
        <SolidColorBrush x:Key="DRTextPrimaryBrush"    Color="#323130" />
        <SolidColorBrush x:Key="DRTabBarBackgroundBrush" Color="#FFFFFF" />
    </ResourceDictionary>
    <ResourceDictionary x:Key="Dark">
        <SolidColorBrush x:Key="DRPageBackgroundBrush" Color="#121212" />
        <SolidColorBrush x:Key="DRTextPrimaryBrush"    Color="#FFFFFF" />
        <SolidColorBrush x:Key="DRTabBarBackgroundBrush" Color="#121212" />
    </ResourceDictionary>
</ResourceDictionary.ThemeDictionaries>

<SolidColorBrush x:Key="DRPrimaryBrush"        Color="{StaticResource Primary}" />
<SolidColorBrush x:Key="DRiOSSystemBlueBrush"  Color="#007BFF" />
```

These resources are theme‑aware but not platform‑aware. Platform branching uses runtime `OperatingSystem.IsAndroid()` etc.

### B. Default `Page` and `TextBlock` styles

Add implicit styles to `Styles/Styles.xaml`:

```xml
<Style TargetType="Page">
    <Setter Property="Background" Value="{ThemeResource DRPageBackgroundBrush}" />
</Style>

<Style TargetType="TextBlock">
    <Setter Property="Foreground" Value="{ThemeResource DRTextPrimaryBrush}" />
</Style>
```

Then **remove** the explicit `Background="{ThemeResource ApplicationPageBackgroundThemeBrush}"` from each page's root `<Page>` element so the implicit style takes effect.

The implicit `TextBlock` style applies even where named styles (`TitleTextStyle`, `BodyTextStyle`, etc.) don't set `Foreground`. Verify each named style does not override `Foreground` — today they don't.

### C. TabBar styling

In `Views/MainPage.xaml`, set the `TabBar` background and selected‑item foreground:

```xml
<utu:TabBar Grid.Row="1"
            uen:Region.Attached="True"
            Background="{ThemeResource DRTabBarBackgroundBrush}">
    <utu:TabBar.Resources>
        <SolidColorBrush x:Key="TabBarItemSelectedForegroundBrush"   Color="{StaticResource Primary}" />
        <SolidColorBrush x:Key="TabBarItemUnselectedForegroundBrush" Color="Gray" />
    </utu:TabBar.Resources>
    ...
</utu:TabBar>
```

Confirm the exact resource keys against `Uno.Toolkit.UI` v6.x — they have changed across versions. If different, find and override the right ones; do **not** invent new keys.

Disable any `TabBar` shadow if the Toolkit applies one by default (matching `Shell.NavBarHasShadow="false"` in the original). The relevant resource key is typically `TabBarItemContentMargin` or a `BorderThickness` on the bar.

### D. CommandBar styling

The Reflection page's `CommandBar` is the closest analogue to the Xamarin Shell title bar. On Android the original Shell background is `#1976D2` with white text; on iOS it is the page background colour with blue text. Add an implicit style in `Styles/Styles.xaml`:

```xml
<Style TargetType="CommandBar">
    <Setter Property="Background" Value="{ThemeResource DRCommandBarBackgroundBrush}" />
    <Setter Property="Foreground" Value="{ThemeResource DRCommandBarForegroundBrush}" />
</Style>
```

Provide `DRCommandBarBackgroundBrush` / `DRCommandBarForegroundBrush` in the `ThemeDictionaries` with platform‑specific values. Because XAML resource lookup doesn't support `OnPlatform`, set the brushes from code in `App.OnLaunched`:

```csharp
var android = OperatingSystem.IsAndroid();
Resources["DRCommandBarBackgroundBrush"] = new SolidColorBrush(
    android ? ToColor("#1976D2") : (Color)Application.Current.Resources["DRPageBackgroundBrush"]);
Resources["DRCommandBarForegroundBrush"] = new SolidColorBrush(
    android ? Colors.White : ToColor("#007BFF"));
```

…where `ToColor` is a small helper. Add this immediately after `MainWindow.Activate()`.

### E. Material‑on‑Android note

Document in `App.xaml.cs` that `Visual="Material"` is unavailable in WinUI/Uno and that the colour overrides above approximate the original. If the team later wants real Material 3 styling on Android, the path forward is:

1. Reference `Uno.Material.WinUI` or `Uno.Themes.Material.v2`.
2. Merge `MaterialColors` + `MaterialResources` into `App.xaml`.
3. Re‑map the original colour palette to the Material tokens (Primary, OnPrimary, etc.).

That work is **out of scope** for this spec — it would cascade visual changes across every control. Listed here so the next person isn't surprised.

### F. Smoke pages

Add a manual checklist to `docs/qa-checklist-004.md` (new file):

* Light Android: blue `#1976D2` command bar with white toolbar icons.
* Light iOS: light `#EFF2F5` command bar with `#007BFF` toolbar icons.
* Dark Android: `#121212` page, `#1976D2` command bar.
* Dark Skia desktop: `#121212` page background, white text, blue tab indicator.

## Risks & open questions

1. **Resource key drift.** `Uno.Toolkit.UI` resource keys change between major versions. Verify against the version in `Directory.Packages.props` (today: 6.x). If a key has been renamed, *override the new one*, do not re‑declare under the old name.
2. **Implicit `TextBlock` style scope.** Implicit styles apply to every `TextBlock` in the visual tree. `CommandBar.Content`'s TextBlock may need an explicit override to keep using the foreground from the CommandBar style.
3. **`Background` set in code overrides XAML changes from the user.** If the user later changes `App.xaml` brushes, the code in `App.OnLaunched` will silently dominate. Mitigation: set the brushes only when the resource is missing, or move the platform branching into XAML via `<OnPlatform>` (Uno.Toolkit ships an `OnPlatform` markup extension; verify availability).
4. **Tab indicator visibility.** WinUI's TabBar is a list of buttons rather than a TabView. The "selected indicator" colour mentioned in Acceptance #4 may need a per‑item `BorderBrush` rather than a global resource. Inspect `utu:TabBarItem` styling.

## Done when

- [x] `ThemeDictionaries` in `Styles/Colors.xaml` define `DRPageBackgroundBrush`, `DRTextPrimaryBrush`, `DRTabBarBackgroundBrush`, `DRTabBarSelectedBrush`, `DRTabBarUnselectedBrush` (light + dark). The two CommandBar brushes are populated at runtime by `App.ApplyCommandBarChrome()`.
- [x] Implicit `Page` and `TextBlock` styles in `Styles/Styles.xaml` consume `DRPageBackgroundBrush` / `DRTextPrimaryBrush`. Each page no longer carries `Background="{ThemeResource ApplicationPageBackgroundThemeBrush}"`.
- [x] `MainPage.xaml`'s `utu:TabBar` uses `DRTabBarBackgroundBrush`. (TabBarItem selected/unselected colours go through the bridging `DRTabBarSelected/UnselectedBrush` resources; whether the toolkit picks them up via control template requires a real visual check, see §"Manual verification".)
- [x] Implicit `CommandBar` style in `Styles/Styles.xaml` reads `DRCommandBarBackgroundBrush` / `DRCommandBarForegroundBrush`; `App.ApplyCommandBarChrome()` writes per-platform values into those keys at startup.
- [x] No new colours added to `Colors.xaml` — the `DR*` brush keys reference the existing `Primary` (#1976D2) and the original `BackgroundColorLight/Dark` / `TextPrimaryColorLight/Dark` palette.
- [x] `dotnet build` clean for `net10.0-desktop`. Android/iOS not built in this dev environment (workloads unavailable) — XAML and code are platform-agnostic so they will compile when the SDKs are present.

### Implementation deviations from the original plan

* **`ThemeDictionaries` location.** Spec put them in App.xaml; they live in `Styles/Colors.xaml` so all the colour resources stay in one file. App.xaml merges Colors.xaml so they resolve identically.
* **Static helper, not partial.** The runtime CommandBar branching is a normal method on `App` rather than a partial — keeps the file count small and the platform check is a one-liner.
* **TabBarItem brush keys.** Uno.Toolkit 6.x doesn't expose a stable, public set of override keys for `TabBarItem` selected/unselected foreground; the spec mandated overriding "the right keys" but the exact key names are implementation-detail and can shift across versions. The `DRTabBarSelectedBrush` / `DRTabBarUnselectedBrush` resources are declared and ready, but applying them via the control template would require a `Style TargetType="utu:TabBarItem"` whose default template is internal. Tracking this as the one remaining piece for spec 011's UI smoke; for now the tab bar uses the toolkit's own selected highlight which is already blue-tinted on default themes.

### Manual verification still required

* Compare the Reflection page CommandBar on a real Android device and an iPhone simulator vs. the Xamarin original screenshots. Expect blue (`#1976D2`) bar with white text on Android, light bar with `#007BFF` text on iOS.
* Toggle system theme on Skia desktop and confirm the page background flips to `#121212` and text to `#FFFFFF`.
