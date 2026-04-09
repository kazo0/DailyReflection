# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is the **Uno Platform port** of the DailyReflection sobriety tracking app. It contains the Uno Platform head (`DailyReflection`) plus the four shared libraries it depends on. There is no MAUI or Avalonia here.

## Build Commands

```bash
# Build everything
dotnet build DailyReflection.slnx

# Run tests
dotnet test DailyReflection.Presentation.Tests
dotnet test DailyReflection.Services.Tests

# Run a single test
dotnet test DailyReflection.Presentation.Tests --filter "FullyQualifiedName~TestMethodName"
```

Targets: `net10.0-android`, `net10.0-ios`, `net10.0-desktop`. Uno.Sdk version is pinned in `global.json`.

## Architecture

### Shared Libraries (dependency order)

```
DailyReflection.Core         → Constants, ServiceCollectionExtensions (reflection-based DI helper)
DailyReflection.Data          → SQLite DB (sqlite-net-pcl), Reflection model, embedded dailyreflections.db
DailyReflection.Services      → ISettingsService, IShareService, INotificationService + DailyReflectionService
DailyReflection.Presentation  → ViewModels (CommunityToolkit.Mvvm), WeakReferenceMessenger messages
```

### Uno Platform Head (`DailyReflection/`)

- **Entry**: `App.OnLaunched()` — builds the DI container manually and navigates a `Frame` to `MainPage`
- **Navigation**: Frame-based; `MainPage` hosts a `TabBar` (Uno.Toolkit) with tabs for DailyReflection, SobrietyTime, and Settings
- **Platform services** in `PlatformServices/`:
  - `SettingsService` — uses `ApplicationData.LocalSettings` (WinUI/Uno equivalent of MAUI Preferences)
  - `ShareService` — uses `DataTransferManager`
  - `NotificationService` — platform-specific files per target (`.Android.cs`, `.iOS.cs`, base `.cs`)

### DI Registration

`App.OnLaunched()` registers everything manually (does **not** call `AddPresentationDependencies()`):
- `IDailyReflectionDatabase` → Singleton
- `IDailyReflectionService` → Transient
- `ISettingsService`, `IShareService` → Singleton
- `INotificationService` → Transient
- ViewModels → Singleton
- Pages → Transient

Services are resolved via `App.GetService<T>()` from pages.

### Key Patterns

- **ViewModelBase** extends `ObservableRecipient`. ViewModels use `[ObservableProperty]` and `[RelayCommand]` source generators.
- **Cross-VM messaging** via `WeakReferenceMessenger` with `SoberDateChangedMessage`, `SoberTimeDisplayPreferenceChangedMessage`, `NotificationPermissionRequestMessage`.
- **Database**: Read-only embedded SQLite extracted to `LocalApplicationData` on first run; queried by Month/Day.
- **Sobriety time** uses NodaTime `Period.Between`.

## Code Style

From `.editorconfig` (inherited via `Directory.Build.props`):
- Tabs (width 4), CRLF line endings
- Explicit types over `var`
- Block-scoped namespaces
- Allman brace style
- `I` prefix for interfaces, PascalCase everywhere else
