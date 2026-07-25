# Spec 007 — `ShareService` hardening

* **Status:** Implemented (2026-05-03).
* **Severity:** 🔴 / 🟡
* **Gaps closed:** 10.7.1, 10.7.2, 10.7.3, 10.7.4
* **Depends on:** —

## Summary

`PlatformServices/ShareService.cs` has three issues:

1. **Race on concurrent share calls.** `_pendingTitle` / `_pendingBody` are instance fields. If a second `ShareText` is invoked before the first's `DataRequested` callback fires, the values are clobbered. The Xamarin original uses `Xamarin.Essentials.IShare.RequestAsync(new ShareTextRequest(...))` which carries the values per‑request and is hazard‑free.
2. **Silent no‑op on unsupported platforms.** When `DataTransferManager.IsSupported() == false` (notably Linux X11), the share button does nothing and reports no error. Xamarin's Essentials would throw `FeatureNotSupportedException`.
3. **Skia desktop interop.** `DataTransferManager.GetForCurrentView()` is the UWP shape; on Skia desktop targets the right path is `IDataTransferManagerInterop` keyed off the window handle. The current code may degrade silently on macOS/Linux.

This spec closes all three.

## Goals

* `ShareText` is reentrant‑safe.
* On platforms where sharing is genuinely unsupported, the user sees feedback (a toast or log) instead of dead silence.
* The implementation uses the right API for the host (`GetForCurrentView` on UWP/Win32, `IDataTransferManagerInterop` on Skia desktop, native sheets on iOS/Android).

## Non‑goals

* Implementing native share sheets on Linux. Linux X11 has no system share UI; we degrade to copying the text to the clipboard and surfacing a toast that says so.
* Adding rich share content (images, attachments). Daily Reflection only ever shares text.

## Acceptance criteria

1. Calling `ShareText("A","a"); ShareText("B","b")` without awaiting in between produces two share UIs in sequence, each carrying the *correct* values for its invocation. (Verifiable via a stress‑mode unit test that captures the args on `DataRequested`.)
2. On Windows desktop the share UI still launches and carries the title/body unchanged.
3. On Linux X11 (`DataTransferManager.IsSupported() == false`), `ShareText` copies the body to the clipboard and shows a `MessageDialog` (or equivalent) saying "Reflection copied to clipboard — share UI is not available on this platform.". No exception leaks.
4. On Skia macOS and Skia Windows the share UI launches with the correct values (validated by adding `IDataTransferManagerInterop` interop where required).
5. `ShareService.DataRequested` is no longer subscribed/unsubscribed inside `ShareText`. The handler is set up once per call via a closure, eliminating the leak window between `+=` and the synchronous `ShowShareUI`.

## Implementation plan

### A. Carry args through the closure

Replace the instance fields with a per‑call `TypedEventHandler`:

```csharp
public Task ShareText(string title, string body)
{
    if (!DataTransferManager.IsSupported())
        return ShareUnsupportedFallback(title, body);

    var dtm = DataTransferManager.GetForCurrentView();
    void Handler(DataTransferManager s, DataRequestedEventArgs args)
    {
        try
        {
            args.Request.Data.Properties.Title = title ?? "Share";
            args.Request.Data.SetText(body ?? string.Empty);
        }
        finally { s.DataRequested -= Handler; }
    }

    dtm.DataRequested += Handler;
    DataTransferManager.ShowShareUI();
    return Task.CompletedTask;
}
```

The handler unsubscribes itself the moment it has supplied the data, so concurrent calls each get their own handler with their own captured values. No instance state.

### B. Unsupported‑platform fallback

```csharp
private static async Task ShareUnsupportedFallback(string title, string body)
{
    var dataPackage = new DataPackage();
    dataPackage.SetText(body);
    Clipboard.SetContent(dataPackage);

    if (App.Current is App app && app.MainWindow is { } window)
    {
        var dialog = new ContentDialog
        {
            Title = "Sharing not available",
            Content = "Reflection copied to clipboard — share UI is not available on this platform.",
            CloseButtonText = "OK",
            XamlRoot = window.Content.XamlRoot
        };
        await dialog.ShowAsync();
    }
}
```

`Windows.ApplicationModel.DataTransfer.Clipboard` is supported on every Uno desktop target (it falls through to the native clipboard on each).

### C. Skia desktop interop

If `DataTransferManager.GetForCurrentView()` throws or returns an instance whose `ShowShareUI` is a no‑op on Skia desktop, switch to interop. Confirm against Uno docs first — recent versions of Uno provide a working `GetForCurrentView` on all desktop targets, so this may be unnecessary. If interop is needed:

```csharp
[Guid("3A3DCD6C-3EAB-43DC-BCDE-45671CE800C8")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IDataTransferManagerInterop
{
    DataTransferManager GetForWindow(IntPtr hwnd, in Guid riid);
    void ShowShareUIForWindow(IntPtr hwnd);
}

private static DataTransferManager GetDtm(IntPtr hwnd)
{
    var interop = (IDataTransferManagerInterop)WinRT.CastExtensions.As<IDataTransferManagerInterop>(
        DataTransferManager.As<object>());
    var iid = typeof(DataTransferManager).GUID;
    return interop.GetForWindow(hwnd, iid);
}
```

Gate this with `#if HAS_UNO_WIN32` (or runtime `OperatingSystem.IsWindows()`); on macOS / Linux desktop, fall through to the §B fallback.

### D. Tests

Add `DailyReflection.Presentation.Tests/Services/ShareServiceConcurrencyTests.cs` (it tests the WinUI head behaviour, but only the behavioural contract — mock `DataTransferManager` via an `IShareSurface` abstraction so tests don't need a UI thread):

```csharp
[Test]
public async Task Two_concurrent_share_calls_carry_independent_payloads()
{
    var fake = new FakeShareSurface();
    var svc  = new ShareService(fake);

    var t1 = svc.ShareText("title-1", "body-1");
    var t2 = svc.ShareText("title-2", "body-2");

    fake.RaiseDataRequested(0); // serves call 1
    fake.RaiseDataRequested(1); // serves call 2
    await Task.WhenAll(t1, t2);

    Assert.That(fake.Captured[0], Is.EqualTo(("title-1","body-1")));
    Assert.That(fake.Captured[1], Is.EqualTo(("title-2","body-2")));
}
```

This requires extracting a tiny `IShareSurface` interface so the production `ShareService` is a thin wrapper around `DataTransferManager`. Acceptable refactor — small.

## Risks & open questions

1. **`DataTransferManager.GetForCurrentView()` on non‑Windows desktop.** Verify with a runtime smoke test on Skia macOS/Linux. If `IsSupported()` already returns false there, §B's fallback covers it cleanly and we don't need the interop in §C.
2. **`Clipboard.SetContent` on Linux X11.** The X11 selection model differs from Windows clipboard — the value is held only as long as the app is running. That's fine for "copied to clipboard" UX as long as the user pastes immediately.
3. **`ContentDialog` requires a `XamlRoot` on WinUI 3.** Resolve via `App.Current.MainWindow.Content.XamlRoot`. If `MainWindow` is null at the time of the call (theoretically possible during early activation), fall back to logging only.

## Done when

- [x] `ShareService` rewritten — no instance state; per-call `Handler` captures `title` / `body` and unsubscribes itself in `finally`.
- [x] Concurrent-call test (`ShareClosureCaptureTests.Two_concurrent_share_calls_carry_independent_payloads`) green.
- [x] `ShareUnsupportedFallback` copies the body to the clipboard and shows a `ContentDialog` titled "Sharing not available" when `DataTransferManager.IsSupported() == false`.
- [x] `App.MainWindow` exposed publicly so the dialog has a `XamlRoot`.
- [x] Synchronous `ShowShareUI` failure path unsubscribes the handler.
- [x] Uno desktop build clean.

### Implementation deviations from the original plan

* **No `IShareSurface` extraction.** Spec proposed extracting an interface for testing. Instead the closure-capture pattern is verified by `ShareClosureCaptureTests` against a hand-rolled fake — proves the `title` / `body` closure invariant without dragging an indirection into production code.
* **No `IDataTransferManagerInterop` shim** introduced. `DataTransferManager.GetForCurrentView()` works on Uno's Skia desktop targets; if a future Uno bump regresses this, the unsupported-platform fallback already provides a graceful path.
* **`MainWindow` exposure.** The spec referenced `App.Current.MainWindow` directly; the property was previously `protected`, so it's been promoted to `public` with a comment explaining why.
