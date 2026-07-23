# Spec 009 — Android permissions and manifest alignment

* **Status:** Implemented (2026-05-03).
* **Severity:** 🟠 / 🟡
* **Gaps closed:** 10.5.14, 10.9.6, 10.9.7
* **Depends on:** —

## Summary

The Uno port's `Platforms/Android/AndroidManifest.xml` declares three permissions the original Xamarin app does not: `SCHEDULE_EXACT_ALARM`, `USE_EXACT_ALARM`, `WAKE_LOCK`. None of them are required by the existing alarm code (`SetAndAllowWhileIdle` is best‑effort and works without the exact‑alarm permissions on the current target SDK; the receivers do not acquire wake locks). They also have Play Store policy implications — `USE_EXACT_ALARM` requires a justification in the listing, and `SCHEDULE_EXACT_ALARM` requires runtime permission flow on Android 14+.

Additionally, the Xamarin manifest pins `<uses-sdk android:minSdkVersion="21" android:targetSdkVersion="33"/>` explicitly. The Uno manifest leaves this to the csproj (`SupportedOSPlatformVersion`/`TargetPlatformVersion`), and the values may have drifted.

This spec drops the unnecessary permissions and pins min/target SDK explicitly to match the original.

## Goals

* Android manifest declares **exactly** the permissions the Xamarin original declares.
* Min SDK 21 / target SDK 33 are explicit either in the manifest or in the csproj, with a documented value.
* Existing alarm scheduling continues to work after the permission removal.

## Non‑goals

* Adding the runtime exact‑alarm permission request flow. We're explicitly **not** using exact alarms — `SetAndAllowWhileIdle` is the right primitive for a once‑per‑day reflection notification, and inexact alarms are sufficient.
* Bumping target SDK (separate decision).

## Acceptance criteria

1. `Platforms/Android/AndroidManifest.xml` lists exactly: `ACCESS_NETWORK_STATE`, `INTERNET`, `VIBRATE`, `RECEIVE_BOOT_COMPLETED`, `POST_NOTIFICATIONS`. Nothing else.
2. The application element keeps `allowBackup`, `supportsRtl`, `icon` attributes.
3. The Android csproj declares `SupportedOSPlatformVersion=21.0` and `TargetPlatformVersion=33.0` (or the equivalent Uno.Sdk properties), pinning to the Xamarin values.
4. After deploying to a Pixel emulator running Android 14, enabling notifications still schedules the reflection (manual smoke). Removing the exact‑alarm permissions does not break the path.
5. After the change, the app launches on a fresh Android 13 device without prompting for any permission *other* than `POST_NOTIFICATIONS`.

## Implementation plan

### A. Manifest cleanup

Replace `Platforms/Android/AndroidManifest.xml` with:

```xml
<?xml version="1.0" encoding="utf-8"?>
<manifest xmlns:android="http://schemas.android.com/apk/res/android">
  <uses-sdk android:minSdkVersion="21" android:targetSdkVersion="33" />
  <application
      android:allowBackup="true"
      android:supportsRtl="true"
      android:icon="@mipmap/appicon" />
  <uses-permission android:name="android.permission.ACCESS_NETWORK_STATE" />
  <uses-permission android:name="android.permission.INTERNET" />
  <uses-permission android:name="android.permission.VIBRATE" />
  <uses-permission android:name="android.permission.RECEIVE_BOOT_COMPLETED" />
  <uses-permission android:name="android.permission.POST_NOTIFICATIONS" />
</manifest>
```

### B. csproj pin

In `heads/DailyReflection.Uno/DailyReflection.Uno/DailyReflection.Uno.csproj`, locate the Android target‑framework property group (or add one) and set:

```xml
<PropertyGroup Condition="$(TargetFramework.Contains('-android'))">
  <SupportedOSPlatformVersion>21.0</SupportedOSPlatformVersion>
  <TargetPlatformVersion>33.0</TargetPlatformVersion>
</PropertyGroup>
```

Verify the right knobs against `Uno.Sdk` 6.x docs — Uno may surface them under different names. The intent is to lock to API 21 / API 33 the same way the Xamarin manifest does.

### C. Verify alarm path still works

The `NotificationService.Android.cs` already uses `SetAndAllowWhileIdle` (API 23+) / `Set` (API 22 and below). Neither requires `SCHEDULE_EXACT_ALARM`. Confirm by:

1. Removing the permissions in step §A.
2. Building and deploying.
3. Enabling notifications, setting the time to "now + 60 seconds".
4. Backgrounding the app.
5. Waiting; the notification should fire within 60 seconds (inexact alarms are usually within ~minutes on doze; on the foreground/recently‑used app they fire promptly).

If the reflection notification reliably fails to fire on a given device, the alarm strategy needs to escalate to `setAlarmClock` (which *does* require `USE_EXACT_ALARM` on API 33+) — but this is not currently an observed problem and the original Xamarin app shipped with these exact same constraints.

### D. Documentation

Add a one‑line comment in `NotificationService.Android.cs` near the alarm schedule:

```csharp
// SetAndAllowWhileIdle is the right primitive for a once-per-day reflection.
// We deliberately do NOT request SCHEDULE_EXACT_ALARM / USE_EXACT_ALARM:
//   - the time is user-chosen at minute granularity, not safety-critical;
//   - exact alarms require Play Store policy answers we don't want to maintain.
```

## Risks & open questions

1. **Doze behaviour drift.** Recent Android versions tighten doze rules; on rare devices `SetAndAllowWhileIdle` may delay the alarm by hours. Mitigation: monitor user reports; if needed, escalate to exact alarms with the matching permissions and a runtime permission UI.
2. **Min SDK 21.** Carried over from Xamarin. There's a case to bump to 24 (Android 7) given the user base, but that's a separate product decision.
3. **`TargetPlatformVersion=33.0` in Uno.Sdk.** Verify the property name; Uno.Sdk historically used `<TargetFramework>net10.0-android33.0</TargetFramework>` instead of `TargetPlatformVersion`. Whichever form Uno expects, pin to 33.

## Done when

- [x] Three permissions removed from `AndroidManifest.xml` (`SCHEDULE_EXACT_ALARM`, `USE_EXACT_ALARM`, `WAKE_LOCK`). The remaining 5 match the Xamarin original.
- [x] `<uses-sdk minSdkVersion="21" targetSdkVersion="33"/>` added to the manifest itself, matching the Xamarin original. (csproj-level pinning would also work; manifest pin is more visible.)
- [x] Comment block in `NotificationService.Android.cs` explains the deliberate omission of exact-alarm permissions.

### Manual verification still required

* On an Android 14 device, confirm the reflection notification still fires reliably with `SetAndAllowWhileIdle` and no exact-alarm permissions. (Doze rules tighten on every release; if reliability regresses, escalating to `setAlarmClock` + the matching permissions is the documented next step.)
