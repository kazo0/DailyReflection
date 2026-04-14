# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

DailyReflection is a sobriety tracking app. The shipping app is **Xamarin.Forms**; this branch (`dev/sb/uno-port`) migrates it to **Uno Platform**. The repo contains the Uno head (`DailyReflection/`) plus four shared `net10.0` libraries it depends on. There is no MAUI or Avalonia code here.

## Build & Test

```bash
# Restore + build the whole solution
dotnet build DailyReflection.slnx

# Build a single platform head (avoids building all TFMs)
dotnet build DailyReflection/DailyReflection.csproj -f net10.0-desktop
dotnet build DailyReflection/DailyReflection.csproj -f net10.0-android
dotnet build DailyReflection/DailyReflection.csproj -f net10.0-ios

# Run desktop head
dotnet run --project DailyReflection -f net10.0-desktop

# Tests
dotnet test DailyReflection.Presentation.Tests
dotnet test DailyReflection.Services.Tests

# Single test
dotnet test DailyReflection.Presentation.Tests --filter "FullyQualifiedName~DailyReflectionViewModelTests.SomeTest"
```

Target frameworks for the head: `net10.0-android`, `net10.0-ios`, `net10.0-desktop`. The `Uno.Sdk` version is pinned in `global.json` — bump it there, not via PackageReference. Package versions are managed centrally in `Directory.Packages.props` (CPM). VS Code tasks `build-wasm`/`build-desktop` exist in `.vscode/tasks.json`, but the WASM TFM is **not** in the head's `<TargetFrameworks>` — that task will fail until added.

## Architecture

### Project graph (dependency order)

```
DailyReflection.Core         Constants, AddAllSubclassesOf<T> reflection-based DI helper
DailyReflection.Data         SQLite (sqlite-net-pcl), Reflection model, embedded dailyreflections.db
DailyReflection.Services     Service interfaces (ISettingsService, IShareService, INotificationService)
                             + DailyReflectionService (the only cross-platform service impl)
DailyReflection.Presentation ViewModels (CommunityToolkit.Mvvm), WeakReferenceMessenger messages
DailyReflection              Uno head: App, Views, PlatformServices, Platforms/{Android,iOS,Desktop}
```

Tests live in `DailyReflection.Presentation.Tests` and `DailyReflection.Services.Tests` — NUnit + Moq, with `ViewModelTestBase<T>` / `ServiceTestBase<T>` providing per-test setup.

### App startup & DI (important gotcha)

There are **two** DI wirings in this repo:

1. `Presentation/DependencyInjection/Dependencies.cs` + `Services/DependencyInjection/Dependencies.cs` + `Data/DependencyInjection/Dependencies.cs` — chained `AddPresentationDependencies()` extension methods that auto-register all `ViewModelBase` subclasses via reflection.
2. `DailyReflection/App.OnLaunched()` — registers everything **manually** and does **not** call `AddPresentationDependencies()`. This is the path actually used at runtime.

When adding a new ViewModel, service, or page, register it explicitly in `App.OnLaunched()` (the auto-registration helper is dead code on the Uno head). Resolve services from pages with `App.GetService<T>()`.

`appsettings.json` is loaded as an embedded resource (`DailyReflection.appsettings.json`) and supplies `DatabaseFileName` to `DailyReflectionDatabase`.

### Navigation

Uno head uses `Frame`-based navigation. `MainPage` hosts a `Uno.Toolkit` `TabBar` whose `SelectionChanged` calls `ContentFrame.Navigate(_pageTypes[index])` over `[DailyReflectionPage, SobrietyTimePage, SettingsPage]`. This replaces the original Xamarin.Forms `TabbedPage` / Shell-style navigation. There is no `Uno.Extensions.Navigation` / route table — pages are referenced by `Type`.

### Platform services

In `DailyReflection/PlatformServices/`:

- `SettingsService` — wraps `Windows.Storage.ApplicationData.LocalSettings` (the Uno equivalent of `Xamarin.Essentials.Preferences`). Stores `DateTime` as `ToBinary()` long; `Get<T>` reverses that.
- `ShareService` — uses `DataTransferManager`.
- `NotificationService` — `partial class` split per target (`NotificationService.Android.cs`, `.iOS.cs`, base `.cs` guarded by `#if !(__ANDROID__ || __IOS__)` as a no-op for desktop).

### Cross-VM communication

ViewModels extend `ViewModelBase : ObservableRecipient`. They use CommunityToolkit.Mvvm source generators (`[ObservableProperty]`, `[RelayCommand]`) and communicate via `WeakReferenceMessenger` with messages defined in `Presentation/Messages/`: `SoberDateChangedMessage`, `SoberTimeDisplayPreferenceChangedMessage`, `NotificationPermissionRequestMessage`.

### Data

`DailyReflectionDatabase` opens an **embedded** read-only SQLite database. On first run it copies `dailyreflections.db` from the `DailyReflection.Data` assembly's manifest resources into `Environment.SpecialFolder.LocalApplicationData`. Reflections are keyed by `(Month, Day)`. Sobriety duration is computed via `NodaTime.Period.Between`.

## Code style

Inherited from `.editorconfig` via `Directory.Build.props`:

- `ImplicitUsings` and `Nullable` enabled solution-wide.
- `csharp_style_namespace_declarations = file_scoped:warning` — use file-scoped namespaces.
- `dotnet_sort_system_directives_first = true`; no `this.` qualification.
- XAML files are UTF-8 BOM, indent 4. JSON/sh use LF; most other files CRLF.
- `Directory.Build.props` suppresses `NU1507`, `NETSDK1201`, `PRI257` — don't try to "fix" them.

## Uno-specific guidance

- This is Uno 6.5+ on `net10.0`. When making non-trivial Uno changes (XAML, hosting, navigation, platform APIs), prefer the `uno_platform_search` / `uno_platform_fetch` MCP tools over guessing — many WinUI APIs differ subtly on Uno.
- The head opts into Uno features via `<UnoFeatures>SkiaRenderer;Hosting;Toolkit;Configuration</UnoFeatures>` in the csproj — adding new Uno capabilities (e.g. `Mvux`, `Extensions`) generally means editing this list rather than adding raw `PackageReference`s.
- `DailyReflection/GlobalUsings.cs` aliases `ApplicationExecutionState` to `Windows.ApplicationModel.Activation.ApplicationExecutionState` — keep that in mind when adding lifecycle code.
