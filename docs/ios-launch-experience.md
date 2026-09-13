# iOS launch experience — design notes

This document records two deliberate decisions made during the iOS Info.plist alignment work in [spec 010](../specs/010-ios-info-plist-alignment.md).

## Minimum OS

The Uno port sets `MinimumOSVersion = 15.0` in `Info.plist` and `SupportedOSPlatformVersion = 15.0` in the iOS target framework's csproj property group. The Xamarin original supported `MinimumOSVersion = 9.0`.

The bump is deliberate. Modern Uno (6.x) targets iOS 15 by default; supporting older OS versions would mean running an older Uno.Sdk and an older .NET runtime. As of 2026, ~99% of active iOS devices run iOS 15 or later, and the original Xamarin codebase is preserved as a git submodule for legacy hardware support if it ever becomes necessary.

## Launch screen

The Xamarin original used a `LaunchScreen.storyboard` (declared via `UILaunchStoryboardName`). The Uno port uses `Uno.Resizetizer` to render the SVG splash defined in `Assets/Splash/splash_logo.svg` into a generated `UnoSplash` storyboard.

This is the recommended path for new Uno apps:

* Removes the storyboard maintenance burden — the splash is regenerated on every build from the single SVG source.
* Produces a comparable user-facing experience (a brief launch image then the first page).
* Avoids carrying an Xcode-only artefact in a `dotnet`-driven build.

Two things in `DailyReflection.Uno.csproj` keep it working on Uno 7:

* **`_AddAppleSplashScreenScales`** adds `@2x`/`@3x` copies of the generated splash image. Resizetizer treats the Skia-rendered iOS head as a Skia app ([uno.resizetizer#377](https://github.com/unoplatform/uno.resizetizer/issues/377)) and only emits `scale-NNN` runtime assets, which UIKit ignores; without the copies the launch image is the 1x file stretched and blurry.
* **The file is not named `splash_screen.svg`.** Its name is the image name in the storyboard, and iOS caches launch images by name across app updates, so test devices kept showing the previous splash. Rename the SVG (and `UnoSplashScreenFile`) again if a future splash change also shows up stale on devices that had an earlier build.

After launch, Uno instantiates the same storyboard in-app until the first page renders and then cross-fades to it, so the image must also look right when drawn by UIKit inside the app.

## Bundle keys

`Info.plist` declares `CFBundleIdentifier`, `CFBundleShortVersionString`, `CFBundleVersion`, `CFBundleName`, and `CFBundleDisplayName` using `$(...)` substitutions that the Uno.Sdk plist transform fills from the csproj `ApplicationId` / `ApplicationDisplayVersion` / `ApplicationVersion` / `ApplicationTitle` properties. CI tools that scrape the plist directly therefore see the same values as the bundle metadata.

If a future Uno.Sdk regression breaks the substitution, drop these keys and rely on the csproj — but at the time of writing, Uno 6.x handles them correctly.

## Orientations

iPhone supports portrait + portrait-upside-down only. iPad supports all four orientations. This matches the Xamarin original (the Uno default had silently allowed iPhone landscape).
