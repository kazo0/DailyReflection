# Spec 010 — iOS Info.plist alignment

* **Status:** Implemented (2026-05-03).
* **Severity:** 🟠
* **Gaps closed:** 10.9.2, 10.9.3, 10.9.4, 10.9.5
* **Depends on:** —

## Summary

The Uno iOS `Info.plist` differs from the Xamarin original in four ways:

1. **Orientations.** Xamarin iPhone allows portrait + portrait‑upside‑down only. Uno iPhone allows portrait + landscape‑left + landscape‑right (no upside‑down, plus landscape). The Sober Time / Reflection content is laid out for portrait; landscape on iPhone produces awkward whitespace.
2. **Minimum OS.** Xamarin: `MinimumOSVersion = 9.0`. Uno: not set in plist; relies on csproj `SupportedOSPlatformVersion` (15.0 by default). Drops everything pre‑iOS 15.
3. **Launch storyboard.** Xamarin: `UILaunchStoryboardName = LaunchScreen`. Uno: replaced with the SVG splash via `Uno.Resizetizer`. Different launch UX.
4. **Bundle metadata.** Xamarin's plist sets `CFBundleIdentifier`, `CFBundleName`, `CFBundleDisplayName`, `CFBundleShortVersionString`, `CFBundleVersion`. Uno's plist leaves these to the csproj `ApplicationId` / `ApplicationDisplayVersion` / `ApplicationVersion`. The result is the same in the built `.ipa`, but tooling that scrapes the plist directly will see fewer keys.

This spec restores items 1 and 2 to match the Xamarin original. Items 3 and 4 are evaluated and a deliberate decision is recorded.

## Goals

* iPhone supports portrait + portrait‑upside‑down only, matching the Xamarin original.
* iPad keeps all four orientations.
* Minimum iOS version is documented (matching either Xamarin's 9.0 or a deliberate newer floor like 15.0 — see §C).
* Launch storyboard story is documented and matches user‑facing intent.

## Non‑goals

* Implementing a custom `LaunchScreen.storyboard` — Uno.Resizetizer's SVG splash is a perfectly serviceable replacement and we're not in the business of maintaining storyboards.
* Re‑enabling iOS 9 — modern Uno requires iOS 15 baseline. The "match Xamarin" goal here is about user‑visible behaviour, not literally supporting decade‑old hardware.

## Acceptance criteria

1. On iPhone, rotating the device while the Uno app is in the foreground does **not** rotate the layout to landscape. Portrait‑upside‑down is allowed.
2. On iPad, rotation works in all four orientations.
3. `MinimumOSVersion` is set explicitly in `Info.plist` to a value the team has signed off on (`15.0` is the recommendation).
4. Splash behaviour: launching the app on a clean install shows the Uno.Resizetizer SVG splash (current behaviour). Documented in `docs/ios-launch-experience.md` as the deliberate choice.
5. `Info.plist` includes `CFBundleShortVersionString` and `CFBundleVersion` keys with `$(ApplicationDisplayVersion)` / `$(ApplicationVersion)` substitutions, so plist‑scraping tools see them.

## Implementation plan

### A. Restore iPhone portrait‑only orientations

Edit `Platforms/iOS/Info.plist`:

```xml
<key>UISupportedInterfaceOrientations</key>
<array>
    <string>UIInterfaceOrientationPortrait</string>
    <string>UIInterfaceOrientationPortraitUpsideDown</string>
</array>

<key>UISupportedInterfaceOrientations~ipad</key>
<array>
    <string>UIInterfaceOrientationPortrait</string>
    <string>UIInterfaceOrientationPortraitUpsideDown</string>
    <string>UIInterfaceOrientationLandscapeLeft</string>
    <string>UIInterfaceOrientationLandscapeRight</string>
</array>
```

### B. Pin minimum OS

Add to `Info.plist`:

```xml
<key>MinimumOSVersion</key>
<string>15.0</string>
```

And mirror in the csproj for the iOS target framework:

```xml
<PropertyGroup Condition="$(TargetFramework.Contains('-ios'))">
    <SupportedOSPlatformVersion>15.0</SupportedOSPlatformVersion>
</PropertyGroup>
```

### C. Document the floor decision

The Xamarin original supported iOS 9.0; Uno cannot. Add to a new file `docs/ios-launch-experience.md`:

> **iOS minimum OS.** The Uno port sets `MinimumOSVersion = 15.0` in `Info.plist`. The Xamarin original supported iOS 9.0. Modern Uno (6.x) targets iOS 15 by default, and supporting older versions would require running an older Uno.Sdk and an older .NET. The team has accepted the bump as a deliberate trade‑off: ~99% of iOS install base is on iOS 15+ at the time of writing, and the original Xamarin codebase is preserved as a submodule for legacy hardware support if it ever becomes necessary.

### D. Document the launch‑screen decision

Same file, additional section:

> **Launch screen.** Xamarin used a `LaunchScreen.storyboard`. The Uno port uses `Uno.Resizetizer` to render the SVG splash defined in `Assets/Splash/splash_screen.svg` at the correct sizes. This is the recommended path for new Uno apps, removes the storyboard maintenance burden, and produces a comparable user‑facing experience (a brief splash screen then the first page). No action needed.

### E. Add bundle keys to plist

Even though the build pipeline injects the values, having them in plist is friendlier to App Store Connect tooling and CI introspection:

```xml
<key>CFBundleIdentifier</key>
<string>$(ApplicationId)</string>

<key>CFBundleShortVersionString</key>
<string>$(ApplicationDisplayVersion)</string>

<key>CFBundleVersion</key>
<string>$(ApplicationVersion)</string>

<key>CFBundleName</key>
<string>$(ApplicationTitle)</string>

<key>CFBundleDisplayName</key>
<string>$(ApplicationTitle)</string>
```

Verify Uno.Sdk's plist transform handles `$(...)` substitutions — recent versions do.

## Risks & open questions

1. **Bundle key substitution.** If Uno.Sdk's plist transform doesn't expand `$(ApplicationId)`, the keys ship literal `$(...)` strings, which breaks signing. Validate by inspecting the built `.app` bundle's `Info.plist`. If substitution doesn't work, drop these keys and rely on the csproj — the original Xamarin plist has them hard‑coded, but doing the same in Uno ties the version to two places.
2. **`MinimumOSVersion` mismatch with csproj.** Both must agree. Set both, not one or the other.
3. **iPhone landscape regression.** If anyone uses the app on an iPhone in landscape today, this change will revert their UX. Acceptable per "match the Xamarin original".

## Done when

- [x] iPhone orientations limited to portrait + portrait‑upside‑down.
- [x] iPad orientations unchanged (portrait + portrait-upside-down + landscape-left + landscape-right).
- [x] `MinimumOSVersion = 15.0` in plist; `SupportedOSPlatformVersion = 15.0` in csproj for the iOS target framework.
- [x] `docs/ios-launch-experience.md` documents the iOS floor, launch-screen, bundle-keys, and orientation decisions.
- [x] `CFBundleIdentifier`, `CFBundleShortVersionString`, `CFBundleVersion`, `CFBundleName`, `CFBundleDisplayName` keys added to plist using `$(...)` substitutions.

### Manual verification still required

* iPhone simulator: rotate the device — no landscape layout (matches Xamarin).
* iPad simulator: rotate — all four orientations work.
* Inspect built `.app` bundle's `Info.plist` and confirm the `$(...)` substitutions resolved to literal values.
