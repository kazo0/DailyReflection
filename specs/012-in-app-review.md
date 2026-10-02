# Spec 012 — In-app rating prompt

* **Status:** Implemented (2026-10-01).
* **Severity:** — (new feature, not a parity gap)
* **Gaps closed:** — (the Xamarin original never asked for ratings)
* **Depends on:** 001 (version tracking)

## Summary

Ask engaged users to rate the app with the platform's own in-app dialog: the Google Play In-App Review API on Android, `SKStoreReviewController` on iOS. Desktop and WebAssembly have no store dialog and do nothing. The OS decides whether the dialog actually appears (both throttle it), so the app only decides when it is *allowed* to ask.

This is the one place the port adds behaviour the Xamarin app did not have. It adds no UI of the app's own, so the "no visual redesign" constraint in `prompt.md` is untouched.

## Goals

* Ask only users who keep coming back, never on day one and never right after an update.
* Never block, slow or crash the app: the request is fire-and-forget and every failure is logged and swallowed.
* Keep the rules in the shared layer and unit-tested; keep platform code to the single API call.

## Non-goals

* A custom "Enjoying Daily Reflection?" pre-prompt or a link to the store page. Both stores discourage gating the dialog on a pre-prompt.
* Knowing whether the user rated. Neither API reports it.

## Acceptance criteria

1. The app records each distinct local calendar day it is used (launch or return from background). Several launches on one day count once.
2. A review is requested only when **all** hold: used on ≥ 5 distinct days; ≥ 14 days since the first recorded use; not the first launch of the current version (`IVersionTrackingService.IsFirstLaunchForCurrentVersion`); ≥ 120 days since the last request (or never requested).
3. The request is made ~10 s after startup (or a return from background) has finished, not while a dialog or flyout is open, and only while the app is in the foreground and uncovered (`IStoreReviewService.CanRequestNowAsync`: Android activity window focus, iOS foreground-active scene). A request skipped for that reason does not start the cooldown.
4. The last-request date is saved *before* the platform is asked, so a failing request still starts the 120-day cooldown. Overlapping attempts (a launch and a resume) ask at most once.
5. Any exception from the review path is logged and swallowed.
6. Desktop and WebAssembly report the feature unsupported and write nothing.

## Implementation

### A. Policy and service (`DailyReflection.Services/Review/`)

* `ReviewPromptPolicy` — pure static `RecordUse(state, today)` and `IsEligible(state, today, isFirstLaunchForCurrentVersion)`; thresholds are public constants. A date earlier than the last recorded use (clock moved back) is ignored.
* `ReviewPromptState` — record: first-use date, last-use date, distinct-day count, last-prompt date.
* `ReviewPromptService : IReviewPromptService` — `RecordUse()` and `TryRequestReviewAsync()` (eligible → platform ready → re-check eligibility → save cooldown → request). "Today" is `DateOnly.FromDateTime(TimeProvider.GetLocalNow().DateTime)`. The clock is the BCL `TimeProvider` (registered as `TimeProvider.System`), not NodaTime: the Services layer has no NodaTime dependency and `TimeProvider` is trivially faked in tests.
* Tests: `DailyReflection.Services.Tests/Review/`.

### B. State persistence (`DailyReflection/PlatformServices/ReviewPromptStateStore.cs`)

`ApplicationData.Current.LocalSettings`, like `VersionTrackingService`, under `DR_RP_FirstUseDate`, `DR_RP_LastUseDate`, `DR_RP_UseDayCount`, `DR_RP_LastPromptDate`. Dates are invariant `yyyy-MM-dd` strings. Not `ISettingsService`: its Android `MirrorSet` copies values into SharedPreferences for the alarm receivers, which have no use for these.

### C. Platform dialog (`DailyReflection/PlatformServices/StoreReviewService.*.cs`)

Not Uno's `StoreContext.RequestRateAndReviewAppAsync()`. On Android it needs the `GooglePlay` UnoFeature, which depends on the deprecated monolithic `Xamarin.Google.Android.Play.Core` 1.10.3. Play Console flags that library as incompatible with `targetSdkVersion` 34+, and this app targets 36. That is filed upstream as [unoplatform/uno#24948](https://github.com/unoplatform/uno/issues/24948). On iOS, Uno makes the same `SKStoreReviewController` call as below, so having both platforms implemented in one place here costs nothing. Revisit once Uno moves to `play:review`.

* **Android** — `Xamarin.Google.Android.Play.Review` (binding of `com.google.android.play:review` 2.0.2), referenced for the Android TFM only. `ReviewManagerFactory.Create(activity)` → `RequestReviewFlow()` → `LaunchReviewFlow(activity, info)`, awaited with `Android.Gms.Extensions.TasksExtensions.AsAsync` from `Xamarin.GooglePlayServices.Tasks`. The binding version is 2.0.2.5: later builds of the same Java library (2.0.2.6+) depend on Play Services Basement versions that pull AndroidX Fragment ≥ 1.8.9, which conflicts with the head's direct `Xamarin.AndroidX.Activity` 1.10.1.3 reference from the Uno SDK (NU1605) and would move dozens of AndroidX and Kotlin packages. 2.0.2.5 adds only the review, Play Core Common, Basement, Tasks and Build.Download packages.
* **iOS** — `SKStoreReviewController.RequestReview(UIWindowScene)` for the foreground-active scene, on the main thread. It is marked obsolete from iOS 18 (`CA1422`, suppressed in place): its replacement, StoreKit's `AppStore.requestReview(in:)`, is Swift-only.
* **Desktop / WebAssembly** — `IsSupported = false`, `CanRequestNowAsync` false.

### D. Trigger (`App.xaml.cs`)

`RequestReviewIfEligibleAsync` runs, not awaited, after `StartupMigrationRunner.RunAsync` (which calls `IVersionTrackingService.Track`, so the first-launch flag is valid) and after each `Resuming`. It records the day of use, waits 10 s, skips if `VisualTreeHelper.GetOpenPopupsForXamlRoot` reports an open dialog or flyout, then calls `TryRequestReviewAsync`. The reflection page itself has no code-behind (`ViewSurfaceTests`), so the shell owns the trigger.

## Risks

* **Android Native AOT + R8.** The store builds are Native AOT and R8-shrunk. A new Java binding is the kind of change that surfaces trimming or JNI-lookup problems (`ClassNotFoundException` / `NoSuchMethodError` in logcat) only at runtime on a device. `alpha.yml` (every app-code merge to `master` → Play internal track + TestFlight) is where it first runs that way.
* **Testing on device.** Play shows the dialog only to an app installed from Play (internal test track or internal app sharing); a sideloaded build completes the flow silently. iOS shows it every time in a debug build, never in TestFlight, and at most three times in 365 days in production.

## Done when

- [x] Policy + service in `DailyReflection.Services/Review/`, with unit tests for each criterion in 1, 2 and 4.
- [x] LocalSettings state store with `DR_RP_` keys.
- [x] Android and iOS implementations; desktop/WASM no-ops.
- [x] Fire-and-forget trigger after startup and on resume, with popup guard and logging.
- [ ] Verified on a Play internal-track install (Android) and a debug build (iOS).
