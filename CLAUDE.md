# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Cross-platform daily reflection/sobriety tracking app built with Uno Platform, sharing a common business logic layer. Ported from the original Xamarin.Forms app (see `master` branch history and `prompt.md`).

## Build Commands

```bash
# Build everything (requires .NET 10 SDK + Uno.Sdk workload)
dotnet build DailyReflection.slnx

# Build only the Uno head + shared projects
dotnet build DailyReflection-uno.slnf

# Build the Uno head for a single target (e.g. desktop, no mobile workloads needed)
dotnet build DailyReflection/DailyReflection.Uno.csproj -f net10.0-desktop

# Run tests (NUnit)
dotnet test DailyReflection.Presentation.Tests
dotnet test DailyReflection.Services.Tests

# Run a single test
dotnet test DailyReflection.Presentation.Tests --filter "FullyQualifiedName~TestMethodName"
```

Note: The Uno build requires the Uno.Sdk (version pinned in `global.json`).

## Architecture

### Layered Shared Libraries

The Uno head depends on the same four shared libraries:

```
DailyReflection.Core         → Constants, extensions (ServiceCollectionExtensions for reflection-based DI)
DailyReflection.Data          → SQLite database (sqlite-net-pcl), Reflection model, embedded dailyreflections.db
DailyReflection.Services      → Service interfaces (ISettingsService, IShareService, INotificationService) + DailyReflectionService impl
DailyReflection.Presentation  → ViewModels (MVVM via CommunityToolkit.Mvvm), Messages (WeakReferenceMessenger)
```

### Platform Head

- **DailyReflection/** — Uno Platform single project (net10.0-android/ios/desktop). Frame-based navigation with TabBar. Entry: `App.OnLaunched()`.

The head provides its own implementations of `ISettingsService`, `IShareService`, and `INotificationService` in a `PlatformServices/` folder.

### DI Registration Chain

```
Platform.AddPlatformServices()       → registers ISettingsService, IShareService, INotificationService
Presentation.AddPresentationDependencies()
  └→ AddAllSubclassesOf<ViewModelBase>()  → reflection-scans assembly for all ViewModel subclasses (Singleton)
  └→ Services.AddServiceDependencies()
      └→ Data.AddDataDependencies()       → IDailyReflectionDatabase (Singleton), IDailyReflectionService (Transient)
```

### Key Patterns

- **ViewModelBase** extends `ObservableRecipient` (CommunityToolkit.Mvvm). ViewModels use `[ObservableProperty]` and `[RelayCommand]` source generators.
- **Cross-VM communication** uses `WeakReferenceMessenger` with typed messages (`SoberDateChangedMessage`, `SoberTimeDisplayPreferenceChangedMessage`, `NotificationPermissionRequestMessage`).
- **Database**: Read-only embedded SQLite (`dailyreflections.db`), queried by Month/Day for daily lookups. Extracted to `LocalApplicationData` on first run.
- **Sobriety time** calculations use NodaTime `Period.Between` for accurate date arithmetic.

## Code Style

Enforced via `.editorconfig`:
- Tabs (width 4), CRLF line endings
- Explicit types preferred over `var` (`csharp_style_var_*: false`)
- Block-scoped namespaces (`csharp_style_namespace_declarations: block_scoped`)
- Allman brace style (new line before all open braces)
- PascalCase for types/members, `I` prefix for interfaces

## Important Constraints

- Changes to Core, Data, Services, or Presentation affect the Uno head — verify accordingly.
- Uno Platform decisions should reference Uno Platform documentation for API mappings.
- The port's design decisions are documented in `specs/` and `docs/ANALYSIS.md`.
