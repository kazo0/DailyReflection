# iOS launch experience — design notes

This document records two deliberate decisions made during the iOS Info.plist alignment work in [spec 010](../specs/010-ios-info-plist-alignment.md).

## Minimum OS

The Uno port sets `MinimumOSVersion = 15.0` in `Info.plist` and `SupportedOSPlatformVersion = 15.0` in the iOS target framework's csproj property group. The Xamarin original supported `MinimumOSVersion = 9.0`.

The bump is deliberate. Modern Uno (6.x) targets iOS 15 by default; supporting older OS versions would mean running an older Uno.Sdk and an older .NET runtime. As of 2026, ~99% of active iOS devices run iOS 15 or later, and the original Xamarin codebase is preserved as a git submodule for legacy hardware support if it ever becomes necessary.

## Launch screen

The Xamarin original used a `LaunchScreen.storyboard` (declared via `UILaunchStoryboardName`). The Uno port uses `Uno.Resizetizer` to render the SVG splash defined in `Assets/Splash/splash_screen.svg` at the correct sizes for each device class.

This is the recommended path for new Uno apps:

* Removes the storyboard maintenance burden — the splash is regenerated on every build from the single SVG source.
* Produces a comparable user-facing experience (a brief launch image then the first page).
* Avoids carrying an Xcode-only artefact in a `dotnet`-driven build.

No additional work needed.

## Bundle keys

`Info.plist` declares `CFBundleIdentifier`, `CFBundleShortVersionString`, `CFBundleVersion`, `CFBundleName`, and `CFBundleDisplayName` using `$(...)` substitutions that the Uno.Sdk plist transform fills from the csproj `ApplicationId` / `ApplicationDisplayVersion` / `ApplicationVersion` / `ApplicationTitle` properties. CI tools that scrape the plist directly therefore see the same values as the bundle metadata.

If a future Uno.Sdk regression breaks the substitution, drop these keys and rely on the csproj — but at the time of writing, Uno 6.x handles them correctly.

## Orientations

iPhone supports portrait + portrait-upside-down only. iPad supports all four orientations. This matches the Xamarin original (the Uno default had silently allowed iPhone landscape).
