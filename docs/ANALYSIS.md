# DailyReflection — Migration Analysis

> Origin: [kazo0/DailyReflection](https://github.com/kazo0/DailyReflection) (Xamarin.Forms).
> This repo houses .NET MAUI and Uno Platform ports of that app, with a fourth Avalonia head (excluded from this report).
> The original Xamarin source is checked in as a git submodule at `heads/DailyReflection.Xamarin/` and is read‑only / unbuilt.

This document inventories the original Xamarin.Forms app, then walks through the two migrations on this branch (MAUI and Uno Platform), and finally enumerates the API‑level deltas, gaps, and new behaviour each port introduced. The Avalonia head is intentionally out of scope.

---

## 1. Executive Summary

| Concern | Xamarin.Forms (original) | MAUI port | Uno Platform port |
|---|---|---|---|
| Shared layer location | `heads/.../DailyReflection.{Core,Data,Services,Presentation}` (submodule, netstandard2.0) | Repo root (`/DailyReflection.{Core,Data,Services,Presentation}`, **net10.0**) | Same shared layer as MAUI |
| MVVM toolkit | `Microsoft.Toolkit.Mvvm` 7.0.0‑preview4 | `CommunityToolkit.Mvvm` 8.4.0 | `CommunityToolkit.Mvvm` 8.4.0 |
| Date math | NodaTime 3.0.3 | NodaTime 3.0.3 | NodaTime 3.0.3 |
| Database | `sqlite-net-pcl` 1.7.335 | `sqlite-net-pcl` 1.10.196‑beta | `sqlite-net-pcl` 1.10.196‑beta + `SQLitePCLRaw.bundle_e_sqlite3` 2.1.10 |
| UI framework | Xamarin.Forms 4.8 + Material Visual | Microsoft.Maui.Controls (net10.0) | WinUI 3 / Uno 6.x (Skia renderer) |
| Navigation | Shell + `<TabBar>` | Shell + `<TabBar>` | `Uno.Toolkit.UI.TabBar` + Uno.Extensions region routing |
| Preferences | `Xamarin.Essentials.IPreferences` (DI‑injected interface) | `Microsoft.Maui.Storage.Preferences.Default` | `Windows.Storage.ApplicationData.LocalSettings` + Android `SharedPreferences` mirror |
| Share | `Xamarin.Essentials.IShare` (DI‑injected interface) | `Microsoft.Maui.ApplicationModel.DataTransfer.Share.Default` | `Windows.ApplicationModel.DataTransfer.DataTransferManager` |
| Notifications | Per‑platform: AlarmManager (Android) / `UNUserNotificationCenter` (iOS) | Same per‑platform APIs, but partial classes co‑located in the head | Same per‑platform APIs; **desktop is a no‑op** |
| Picker styling | `CustomDatePickerRenderer` / `CustomTimePickerRenderer` (custom renderers) | `DatePickerHandler.Mapper.AppendToMapping` in `App.xaml.cs` (handlers) | Native `CalendarDatePicker` / `TimePicker` flyouts (no customisation needed) |
| Target frameworks (head) | netstandard2.0 + `MonoAndroid` + `Xamarin.iOS` | `net10.0-android;net10.0-ios;net10.0-maccatalyst` | `net10.0-android;net10.0-ios;net10.0-desktop` |
| Test projects | `*.Tests` (netstandard2.0, NUnit + Moq + Xamarin.Essentials.Interfaces) | `*.Tests` (**net9.0**, NUnit + Moq) | Same shared test projects (used via Presentation reference) |

Both ports preserve the same three‑tab UX (Daily Reflection / Sober Time / Settings), the same view models and messages, and the same two persistence concerns (read‑only embedded SQLite database + key/value preferences). What differs is the platform glue — controls, navigation host, preference store, share API, and how custom picker chrome is achieved.

---

## 2. Original Xamarin.Forms app — feature inventory

### 2.1 Solution shape

`heads/DailyReflection.Xamarin/DailyReflection.sln` contains four shared netstandard2.0 libraries and three platform / test heads:

```
DailyReflection.Core           // Constants, ServiceCollectionExtensions, StringExtensions
DailyReflection.Data           // Reflection model, embedded dailyreflections.db, IDailyReflectionDatabase
DailyReflection.Services       // IDailyReflectionService, ISettingsService, IShareService, INotificationService
DailyReflection.Presentation   // ViewModelBase + 3 ViewModels, 3 Messages, ReflectionExtensions

DailyReflection                 // Xamarin.Forms shared (XAML pages, App.xaml, Startup.cs)
DailyReflection.Android         // MonoAndroid head
DailyReflection.iOS             // Xamarin.iOS head
DailyReflection.UITests
DailyReflection.Presentation.Tests
DailyReflection.Services.Tests
```

### 2.2 NuGet footprint of note

* `Xamarin.Forms` 4.8.0.1821 + `Xamarin.Forms.Visual.Material` (Material Design on Android via shell `Visual="Material"`).
* `Xamarin.Essentials` 1.7.5 + `Xamarin.Essentials.Interfaces` 1.7.4 — used through their interfaces (`IPreferences`, `IShare`) registered in DI rather than via the static `Preferences`/`Share` types. This is what made the service layer testable without a platform.
* `Xamarin.CommunityToolkit` 1.0.0‑pre5 — supplies `IntToBoolConverter`, `IsNotNullOrEmptyConverter`, `EventToCommandBehavior`.
* `Microsoft.Toolkit.Mvvm` 7.0.0‑preview4 — `ObservableRecipient`, `RelayCommand`, `WeakReferenceMessenger`.
* `Microsoft.Extensions.Hosting` 5.0 — full generic host inside a Xamarin.Forms app for DI + configuration.
* `NodaTime` 3.0.3 for sober‑period arithmetic.
* `sqlite-net-pcl` 1.7.335 for the embedded database.

### 2.3 Pages, controls, and navigation

`heads/DailyReflection.Xamarin/DailyReflection/DailyReflection/Views/`:

* **`AppShell.xaml`** — `Shell` with `<TabBar>` containing three `<Tab>` entries (Reflection, Sober Time, Settings). `Visual="Material"` for native Material on Android. Per‑platform tab‑bar colours via `OnPlatform`.
* **`DailyReflectionView.xaml`** — `ContentPage` with `ScrollView` + `StackLayout`, `ActivityIndicator`, hidden `CustomDatePicker` driven via `EventToCommandBehavior` + `DateChangedEventArgsConverter`. `Label TextType="Html"` renders the `<i>…</i>` markup that lives in the database. Toolbar items: calendar (focuses the hidden picker) and share (`ShareCommand`).
* **`SobrietyTimeView.xaml`** — `ScrollView` + `Grid` showing either *Years/Months/Days* or *Days only* depending on `SoberTimeDisplayPreference`. Pluralisation via `PluralityConverter`. Calendar glyph via `FontImage`.
* **`SettingsView.xaml`** — `TableView` (`Intent="Settings"`) with `SwitchCell` + `TextCell` rows. Hidden `CustomDatePicker` / `CustomTimePicker` / `Picker` are focused programmatically when the user taps the corresponding cell. Registers `NotificationPermissionRequestMessage` in code‑behind to surface a permission alert.

Custom controls: `CustomDatePicker` / `CustomTimePicker` are empty subclasses purely so platform renderers can target them without affecting every other Xamarin.Forms picker.

### 2.4 Cross‑cutting application code

* **`App.xaml.cs`** wires `MainPage = ServiceProvider.GetService<AppShell>()`, calls `VersionTracking.Track()` (Xamarin.Essentials), and on `OnStart` runs two version‑gated migrations: `MigrateSettingsIfNeeded` (build < 20 → re‑seat preferences via `ISettingsService.MigrateOldPreferences`) and `RefreshDatabaseIfNeeded` (build < 32 → `IDailyReflectionDatabase.RefreshDatabaseFile()`).
* **`Startup.cs`** uses `Host.CreateDefaultBuilder()`, registers `appsettings.json` via `EmbeddedFileProvider`, calls `services.AddPages()` (auto‑registers all `Page` subclasses via `AddAllSubclassesOf<Page>`), then `AddPresentationDependencies()` which walks down through Services and Data.
* **DI chain** — `Presentation.AddPresentationDependencies` ⇒ `AddAllSubclassesOf<ViewModelBase>` (Singleton) ⇒ `Services.AddServiceDependencies` ⇒ `Data.AddDataDependencies`. `Services.AddServiceDependencies` also registers `Xamarin.Essentials.ShareImplementation` and `PreferencesImplementation` so the `ISettingsService` / `IShareService` wrappers can take `IPreferences` / `IShare` constructor parameters.

### 2.5 Platform services

| Service | Android implementation | iOS implementation |
|---|---|---|
| `INotificationService` | `Services/NotificationService.cs` using `AlarmManager` + `PendingIntent` + `BroadcastReceivers/DailyNotificationReceiver` (which re‑arms the next day's alarm and posts the `NotificationCompat.Builder` notification). Permissions via `Xamarin.Essentials.Permissions` (custom `NotificationPermission : BasePlatformPermission` for Android 13+ `POST_NOTIFICATIONS`). `WakeUpAlarmReceiver` listens for `BOOT_COMPLETED` and `ScheduleExactAlarmPermissionStateChanged` to re‑arm. | `Services/NotificationService.cs` using `UNUserNotificationCenter` + `UNCalendarNotificationTrigger` (repeating). `ShowNotificationSettings` opens `UIApplication.OpenNotificationSettingsUrl`. |
| `ISettingsService` | Cross‑platform; injects `Xamarin.Essentials.IPreferences` and switches over `T` for `bool/int/double/float/long/string/DateTime`. | Same. |
| `IShareService` | Cross‑platform; injects `Xamarin.Essentials.IShare` and calls `RequestAsync(new ShareTextRequest(...))`. | Same. |

Renderers customise picker chrome:

* `CustomDatePickerRenderer` (Android) clears the underline drawable; (iOS) zeroes border, sets `UIDatePickerStyle.Inline` on iOS 13.4+.
* `CustomTimePickerRenderer` (Android) clears the underline; (iOS) zeroes border, forces `UIDatePickerStyle.Wheels` to keep the wheel UX.

### 2.6 Manifests / capabilities

`AndroidManifest.xml`: package `com.kazo0.dailyreflection`, version 3.4 (34), `minSdk 21`, `targetSdk 33`, permissions `ACCESS_NETWORK_STATE`, `INTERNET`, `VIBRATE`, `RECEIVE_BOOT_COMPLETED`, `POST_NOTIFICATIONS`.

`Info.plist`: bundle id `com.kazo0.dailyreflection`, version 3.4 (34), iPhone+iPad, min iOS 9.0, FontAwesome OTFs registered, `LaunchScreen.storyboard`.

### 2.7 Tests

`DailyReflection.Presentation.Tests` and `DailyReflection.Services.Tests` cover ViewModel behaviour and service plumbing with NUnit + Moq + `Xamarin.Essentials.Interfaces` (the testable IPreferences/IShare facades). Both projects target netstandard2.0.

---

## 3. Shared‑library evolution

The MAUI and Uno ports share **one** copy of Core/Data/Services/Presentation at the repo root. Comparing it against the Xamarin original:

| Layer | Xamarin (netstandard2.0) | This repo (net10.0) |
|---|---|---|
| `Core` | `ServiceCollectionExtensions.AddAllSubclassesOf<T>`, `StringExtensions.StripHtml`, constants. | Identical surface. Now `net10.0`; uses `System.Web.HttpUtility` via the modern BCL. |
| `Data` | `Reflection`, `IDailyReflectionDatabase`, embedded `dailyreflections.db`. | Identical. `sqlite-net-pcl` upgraded to 1.10.196‑beta. |
| `Services` | `IDailyReflectionService` + impl, `ISettingsService` + cross‑platform impl injecting `IPreferences`, `IShareService` + cross‑platform impl injecting `IShare`, `INotificationService` (interface only), `AddServiceDependencies` registers all of the above plus `Xamarin.Essentials.{Preferences,Share}Implementation`. | Same interfaces; **the cross‑platform `SettingsService` and `ShareService` implementations move out of `Services` and into the per‑head `PlatformServices/` folder.** `Services.AddServiceDependencies` now only registers `IDailyReflectionService` + the data layer. |
| `Presentation` | 3 ViewModels + base, 3 messages, `ReflectionExtensions`, `AddPresentationDependencies`. | Identical surface. `Microsoft.Toolkit.Mvvm` 7.0‑preview4 → `CommunityToolkit.Mvvm` 8.4.0 (renamed namespace, source‑generator MVVM). |

A grep across the four shared projects for `#if WINDOWS / __ANDROID__ / __IOS__ / __WASM__ / HAS_UNO` returns zero hits — the shared layer stayed truly platform‑agnostic. All platform variation lives in the head.

The ViewModel API (`DailyReflectionViewModel`, `SettingsViewModel`, `SobrietyTimeViewModel`) is unchanged: same `[ObservableProperty]` properties, same commands, same messages (`SoberDateChangedMessage`, `SoberTimeDisplayPreferenceChangedMessage`, `NotificationPermissionRequestMessage`).

> **Note on tests.** The two `*.Tests` projects target `net9.0` while the shared libraries are `net10.0`; per `CLAUDE.md` they are intentionally not part of the main solution and must be built / run separately (`dotnet test DailyReflection.Presentation.Tests`).

---

## 4. MAUI port (`heads/DailyReflection.Maui/`)

### 4.1 Project shape

* `net10.0-android;net10.0-ios;net10.0-maccatalyst`. Single project. `ApplicationId = com.kazo0.dailyreflection-maui`, version 1.0.
* Packages: `Microsoft.Maui.Controls` (via `MauiVersion`), `CommunityToolkit.Maui` 12.2.0, `Microsoft.Extensions.Configuration.{Json,Binder}` 10.0.0‑rc.1.
* Assets: `Resources/AppIcon/appicon.svg`, `Resources/Splash/splash.svg`, `Resources/Fonts/{OpenSans-*.ttf, FontAwesome*.otf}`, `Resources/Images/notif_icon.png`. `appsettings.json` is embedded.

### 4.2 App composition

* `MauiProgram.cs` builds the `MauiApp` with `UseMauiApp<App>()` + `UseMauiCommunityToolkit()`, registers fonts (`OpenSansRegular`, `OpenSansSemibold`, `FaBrandsFont`, `FaRegularFont`, `FaSolidFont`), embeds `appsettings.json` via `Assembly.GetManifestResourceStream(...)` + `ConfigurationBuilder().AddJsonStream(...)`, and exposes the resulting `MauiApp` and `IServiceProvider` as static properties on `MauiProgram`.
* DI: `services.AddPages()` (auto‑register `Page` subclasses, Singleton) → `services.AddPlatformServices()` (registers MAUI versions of `ISettingsService`, `IShareService`, `INotificationService`) → `services.AddPresentationDependencies()` (shared chain).
* `App.xaml.cs` overrides `CreateWindow` to return `new Window(serviceProvider.GetService<AppShell>())`, calls `VersionTracking.Track()`, and on `OnStart` runs the same `MigrateSettingsIfNeeded` / `RefreshDatabaseIfNeeded` logic as the original. It also customises picker handlers via `Microsoft.Maui.Handlers.DatePickerHandler.Mapper.AppendToMapping(...)` / `TimePickerHandler.Mapper...` — eliminating the renderer subclasses.

### 4.3 UI APIs (vs. Xamarin)

| Concern | Xamarin | MAUI |
|---|---|---|
| Page base class | `ContentPage` | `ContentPage` (same XAML; just `Microsoft.Maui.Controls` namespace). |
| Navigation root | `Shell` + `<TabBar>` + `<Tab>` (`Visual="Material"`). | `Shell` + `<TabBar>` + `<Tab>` (`Visual="Material"`) — virtually unchanged. |
| Toolbar | `<ContentPage.ToolbarItems>` + `ToolbarItem` (Primary/Secondary). | Identical. |
| Settings rows | `TableView`/`SwitchCell`/`TextCell`. | **Identical.** MAUI still ships these, so `SettingsView.xaml` is reused with only namespace changes. |
| Hidden pickers | `CustomDatePicker`/`CustomTimePicker` subclasses + custom renderers. | Same subclasses (`Controls/CustomDatePicker.cs` empty) but **custom renderers replaced with handler `Mapper.AppendToMapping`** in `App.xaml.cs` (iOS: `BorderStyle = None`, inline date style; Android: clear background). |
| Behaviors | `xct:EventToCommandBehavior` (Xamarin Community Toolkit). | `mct:EventToCommandBehavior` (MAUI Community Toolkit, namespace flipped to `CommunityToolkit.Maui`). |
| HTML rendering | `Label TextType="Html"`. | Identical. |
| Converters | 9 converters in the head. | Identical 9 converters; same logic. |
| Styling | App.xaml resource dictionary with FontImage glyph factories, primary blue `#1976D2`. | Same dictionary, but split into `Resources/Styles/Colors.xaml` + `Resources/Styles/Styles.xaml`. Adds the standard MAUI template's neutral palette (Primary/Secondary/Tertiary/Gray100‑950) on top of the original blue, plus `Headline`/`SubHeadline` text styles. |

### 4.4 Non‑UI APIs (vs. Xamarin)

| Concern | Xamarin (Essentials) | MAUI head |
|---|---|---|
| Preferences | `IPreferences.Get<T>/Set<T>` (DI‑injected `PreferencesImplementation`). Cross‑platform impl in **`Services` library**. | **`Microsoft.Maui.Storage.Preferences.Default.Get/Set`** called directly from `PlatformServices/SettingsService.cs` in the head. No more `IPreferences` indirection in DI. |
| Share | `IShare.RequestAsync(new ShareTextRequest(body, title))`. | `Microsoft.Maui.ApplicationModel.DataTransfer.Share.Default.RequestAsync(new ShareTextRequest(text, title))` from `PlatformServices/ShareService.cs`. |
| Version tracking | `Xamarin.Essentials.VersionTracking.Track()`. | `Microsoft.Maui.ApplicationModel.VersionTracking.Track()`. |
| Notifications (iOS) | `UNUserNotificationCenter` in `DailyReflection.iOS/Services/NotificationService.cs`. | Same APIs (`UNUserNotificationCenter`, `UNMutableNotificationContent`, `UNCalendarNotificationTrigger`) but now compiled into the single project under `PlatformServices/NotificationService.iOS.cs`. |
| Notifications (Android) | `AlarmManager.Set/SetAndAllowWhileIdle` + `PendingIntent.GetBroadcast` + `DailyNotificationReceiver` + `WakeUpAlarmReceiver`. | Identical. `PlatformServices/NotificationService.Android.cs` calls `Permissions.PostNotifications` (now via `Microsoft.Maui.ApplicationModel`). Receivers live in `Platforms/Android/BroadcastReceivers/`. The receiver reads the same `DR_Settings` `SharedPreferences` keys that `SettingsService` writes through `Preferences.Default`. |
| Permissions | `Xamarin.Essentials.Permissions` + custom `BasePlatformPermission` for `POST_NOTIFICATIONS`. | `Microsoft.Maui.ApplicationModel.Permissions.PostNotifications` (built in to MAUI Essentials). |
| DI | `Microsoft.Extensions.Hosting` generic host. | MAUI's built‑in `MauiAppBuilder` (which uses the same `Microsoft.Extensions.DependencyInjection`). |

### 4.5 Manifests

* `Platforms/Android/AndroidManifest.xml` keeps the same five permissions as Xamarin (`ACCESS_NETWORK_STATE`, `INTERNET`, `VIBRATE`, `RECEIVE_BOOT_COMPLETED`, `POST_NOTIFICATIONS`).
* iOS / macCatalyst: minimum 15.0 (was 9.0 on Xamarin). `Info.plist` registers the same FontAwesome OTFs.
* macCatalyst is genuinely a new target (the Xamarin original was iPhone+iPad only).

### 4.6 Net‑new in MAUI vs. original

* macCatalyst target.
* Handler‑based picker customisation (`AppendToMapping`) replacing the two custom renderer files.
* Picker chrome style updates (no border, inline date picker on iOS, cleared background on Android) live in *one* place rather than per‑renderer.
* `Resources/Splash/splash.svg` + `Resources/AppIcon/appicon.svg` — proper MAUI single‑project icon/splash pipeline.
* Default MAUI palette and typography styles overlay the original blue theme. Visually no redesign, but the resource dictionary surface is broader.

### 4.7 Things lost / different in MAUI vs. original

* `IPreferences` / `IShare` interfaces are **no longer injected**; the cross‑platform `SettingsService` / `ShareService` now talk to the static `Preferences.Default` / `Share.Default` directly. This removes a unit‑test seam that the original carried via `Xamarin.Essentials.Interfaces`.
* The original Xamarin `MigrateOldPreferences` would migrate from a *different* preferences container; the MAUI implementation calls `Preferences.Default.Clear()` after re‑setting the values, which is semantically the same migration but worth re‑reading on the next Android version bump.
* The MAUI head no longer has any custom renderers (renderers are deprecated in MAUI), and the `Visual="Material"` Shell attribute still appears on `AppShell.xaml`, but in MAUI this is a no‑op rather than an active styling mechanism — Android styling is now driven entirely by `Styles.xaml` + the OS Material 3 theme.

---

## 5. Uno Platform port (`heads/DailyReflection.Uno/`)

The Uno port was generated by following `prompt.md` (see `genlog.md` for the full migration log; `genlog.md` itself is the verbatim transcript of the migration session, including each Uno Docs MCP query that backed a control‑mapping decision). The port keeps the same shared layer, the same view models, the same converters' *behaviour*, and the same three‑tab UX, but rebuilds the platform glue on the Uno.Extensions stack.

### 5.1 Project shape

* `Uno.Sdk` 6.4.58 in `heads/DailyReflection.Uno/global.json` (the repo‑root `global.json` pins 6.5.31; the Uno head's local `global.json` overrides).
* `TargetFrameworks = net10.0-android;net10.0-ios;net10.0-desktop`. **Desktop is new** — Skia‑rendered Windows / macOS / Linux. There is no macCatalyst target.
* `<UnoFeatures>SkiaRenderer; Hosting; Toolkit; Configuration; Navigation;</UnoFeatures>` — pulls in `Uno.Extensions.Hosting`, `Uno.Extensions.Configuration`, `Uno.Extensions.Navigation`, and `Uno.Toolkit.UI`.
* `ApplicationId = com.kazo0.dailyreflectionuno`.
* Central package management via `Directory.Packages.props`: `CommunityToolkit.Mvvm` 8.4.0, `NodaTime` 3.0.3, `sqlite-net-pcl` 1.10.196‑beta, `SQLitePCLRaw.bundle_e_sqlite3` 2.1.10.

### 5.2 App composition

`App.xaml.cs` (recently re‑read to confirm) chains:

```csharp
var builder = this.CreateBuilder(args)
    .UseToolkitNavigation()
    .Configure(host => host
        .UseConfiguration(c => c.EmbeddedSource<App>())
        .ConfigureServices((ctx, s) =>
        {
            s.AddPlatformServices();
            s.AddPresentationDependencies();
            s.AddTransient<MainPage>();
            s.AddTransient<DailyReflectionPage>();
            s.AddTransient<SettingsPage>();
            s.AddTransient<SobrietyTimePage>();
        })
        .UseNavigation(RegisterRoutes));
MainWindow = builder.Window;
MainWindow.SetWindowIcon();
Host = await builder.NavigateAsync<MainPage>();
```

`RegisterRoutes` declares one root route (`""` → `MainPage`) with three nested routes (`Reflection` (default), `SoberTime`, `Settings`). The pages are registered as Transient (in contrast to MAUI's `AddPages()` which registers them Singleton); the ViewModels remain Singleton via the shared `AddAllSubclassesOf<ViewModelBase>` call.

### 5.3 UI APIs (vs. MAUI)

| Concern | MAUI | Uno |
|---|---|---|
| Page base | `Microsoft.Maui.Controls.ContentPage`. | `Microsoft.UI.Xaml.Controls.Page` (WinUI). |
| Navigation root | `Shell` + `<TabBar>` + `<Tab>`. | `Uno.Toolkit.UI.TabBar` + `Uno.Extensions.Navigation` *regions*. `MainPage.xaml` declares an outer `Grid` with `uen:Region.Attached="True"`, an inner content `Grid` with `uen:Region.Navigator="Visibility"`, and three `<utu:TabBarItem uen:Region.Name="…">` entries that map to the nested routes. There is no `Frame` and no Shell. |
| Tab content lifetime | Each tab page held by Shell in memory. | The `Visibility` region navigator toggles `Visibility` on the materialised tab, so all three tab pages stay in memory (similar lifetime). |
| Toolbar | `ContentPage.ToolbarItems`. | `CommandBar` + `AppBarButton` (with `CommandBar.SecondaryCommands` for the Share overflow). The page title moves into `CommandBar.Content` because there is no Shell title to bind. |
| Date selection | Hidden `CustomDatePicker` focused on toolbar tap. | `AppBarButton.Flyout = DatePickerFlyout` (modal flyout) with a `DatePicked` handler. |
| Settings rows | `TableView` / `SwitchCell` / `TextCell`. | Plain `Grid`s styled to look like cards (using `CardBackgroundFillColorDefaultBrush`). Tapping reveals a previously‑collapsed `TimePicker` / `CalendarDatePicker` / `ComboBox`. **There is no `TableView` equivalent in WinUI/Uno**, so the layout was hand‑rolled. |
| Toggle | `SwitchCell`. | `ToggleSwitch`. |
| Picker | `Picker` + `SoberTimeDisplayEnumConverter`. | `ComboBox` bound directly to the enum list. |
| Date picker | `DatePicker` (custom subclass for renderer/handler hooks). | `CalendarDatePicker` (no customisation needed). |
| Time picker | `TimePicker` (custom subclass). | `TimePicker` (WinUI primitive). |
| Activity indicator | `ActivityIndicator`. | `ProgressRing`. |
| `ScrollView` | `ScrollView`. | `ScrollViewer`. |
| `StackLayout` | `StackLayout`. | `StackPanel`. |
| `Label` | `Label` with `TextType="Html"` for the `<i>` reflection markup. | `TextBlock` + a new **`HtmlToTextConverter`** that strips tags via `StringExtensions.StripHtml`. The reading is rendered in plain text with `FontStyle="Italic"` applied at the control level — the original `<i>` markup no longer flows through. |
| Bindings | `{Binding}` (with classic data binding). | `{x:Bind …, Mode=OneWay}` (compiled bindings) with helper methods on the page (`FormatPageDate`, `FormatNotificationTime`, `ShowContent`) for inline formatting that classic binding could do via `StringFormat=`. |
| Converters | 9 converters. | 11 converters: keeps all 9 (with WinUI `IValueConverter`), adds **`NullToBoolConverter`**, **`BoolToVisibilityConverter`** (with an `Invert` property), **`AllFalseBoolToVisibilityConverter`**, and **`HtmlToTextConverter`**. The MAUI `IsDisplayPreferenceToBoolConverter` is preserved by name. The Xamarin Community Toolkit converters that were previously merged into App.xaml (`IntToBoolConverter`, `IsNotNullOrEmptyConverter`) become first‑class types in `Converters/`. |
| FontImage | `FontImage` resource (`FaSolidFont` etc.). | `FontIcon` element with `FontFamily="{StaticResource FontAwesomeSolid}"` + `Glyph="…"`; the FontFamily references are declared in `App.xaml`. |
| Theme | `AppThemeBinding` + Material visual on Shell. | `ThemeResource` lookups against `XamlControlsResources` (WinUI defaults) merged with `Uno.Toolkit.ToolkitResources` and the local `Colors.xaml`/`Styles.xaml`. The `#1976D2` primary is preserved. |

### 5.4 Non‑UI APIs (vs. MAUI)

| Concern | MAUI head | Uno head |
|---|---|---|
| Preferences | `Microsoft.Maui.Storage.Preferences.Default`. | `Windows.Storage.ApplicationData.Current.LocalSettings` in `PlatformServices/SettingsService.cs`. **`DateTime` is round‑tripped via `DateTime.ToBinary()` / `DateTime.FromBinary()`** because `LocalSettings` only persists a closed list of primitives. **Android also writes through to `SharedPreferences`** in `SettingsService.Android.cs` (`partial void MirrorSet<T>`) so that `DailyNotificationReceiver` / `WakeUpAlarmReceiver` can read settings without booting `ApplicationData`. |
| Share | `Share.Default.RequestAsync`. | `Windows.ApplicationModel.DataTransfer.DataTransferManager.GetForCurrentView()` + `DataRequested` handler. **`DataTransferManager.IsSupported()` is checked first; on unsupported platforms (notably Linux X11 and Android in some contexts) `ShareText` returns `Task.CompletedTask`** rather than throwing. |
| Notifications (iOS) | `UNUserNotificationCenter` in head. | Same APIs in `PlatformServices/NotificationService.iOS.cs`. |
| Notifications (Android) | `AlarmManager` + `DailyNotificationReceiver` + `WakeUpAlarmReceiver`. | Same APIs in `PlatformServices/NotificationService.Android.cs` + receivers under `Platforms/Android/BroadcastReceivers/`. The receivers read the mirrored Android `SharedPreferences` instead of `Preferences.Default`. |
| Notifications (desktop) | (no desktop target) | `PlatformServices/NotificationService.cs` (the un‑guarded file) is a **no‑op stub** — `CanScheduleNotifications` returns false, `TryScheduleDailyNotification` returns false. This is the single biggest behavioural gap in the desktop port. |
| Permissions | `Microsoft.Maui.ApplicationModel.Permissions`. | Equivalent helpers from Uno; on Android 13+ checks `Manifest.Permission.PostNotifications`. |
| Version tracking | `VersionTracking.Track()` + version‑gated migrations in `App.OnStart`. | **Not present in the Uno port.** `App.OnLaunched` does not call `VersionTracking.Track()` and there is no `MigrateSettingsIfNeeded` / `RefreshDatabaseIfNeeded` step. New Uno installs work because the database is extracted on first run regardless, but a future schema migration would have to re‑introduce the gate. |
| DI / configuration | `MauiAppBuilder` + `builder.Configuration.AddConfiguration(...)`. | `Uno.Extensions.Hosting` `IHostBuilder` with `UseConfiguration(c => c.EmbeddedSource<App>())`. Same `appsettings.json` layout. |
| Navigation API | `Shell.Current.GoToAsync`. | `INavigator.NavigateRouteAsync` from `Uno.Extensions.Navigation` (currently unused for declarative tab switching, since `TabBar` regions handle it directly). |

### 5.5 Manifests & capabilities

* `Platforms/Android/AndroidManifest.xml` keeps the five MAUI permissions and **adds three more**: `SCHEDULE_EXACT_ALARM`, `USE_EXACT_ALARM`, `WAKE_LOCK`. These are required for `setExactAndAllowWhileIdle` on Android 12+ to behave like the original on devices where exact alarms are not pre‑granted; Xamarin and MAUI got away without them because they were targeting older API levels.
* iOS `Info.plist`: iPhone+iPad, the same FontAwesome OTFs registered, `UIApplicationSupportsIndirectInputEvents` true. No `LaunchScreen.storyboard`; Uno's splash is driven by `Assets/Splash/splash_screen.svg` via `Uno.Resizetizer`.
* `Package.appxmanifest` (Windows): targets `Windows.Universal` + `Windows.Desktop`, min 10.0.17763.0, declares `runFullTrust`. Used only for the Windows desktop target (Uno desktop on Windows).
* Desktop entry point in `Platforms/Desktop/Program.cs` chains `.UseX11().UseLinuxFrameBuffer().UseMacOS().UseWin32()` so a single `net10.0-desktop` build runs on Win32, macOS, X11, and Linux framebuffer.

### 5.6 Net‑new in Uno vs. MAUI / original

* **Desktop target** (Windows Win32 + macOS + Linux X11/framebuffer) via Skia. MAUI doesn't ship a desktop story without macCatalyst or WinUI3.
* **Compiled (`x:Bind`) bindings** throughout the views, plus formatter helpers in code‑behind (`FormatPageDate`, `FormatNotificationTime`, `FormatSoberDate`).
* `HtmlToTextConverter`, `NullToBoolConverter`, `BoolToVisibilityConverter` (with `Invert`), and `AllFalseBoolToVisibilityConverter` to cover the converter gaps WinUI doesn't ship.
* Dual settings store on Android (`LocalSettings` + `SharedPreferences` mirror) to support the broadcast receivers without dragging in `Windows.Storage` from a background context.
* Three additional Android manifest permissions for exact alarm scheduling.
* `Uno.Resizetizer` SVG asset pipeline for icon/splash.
* Localisation scaffolding (`Strings/en/Resources.resw`) — currently only carries `ApplicationName`, but the project structure is now ready for additional locales (MAUI ships none).

### 5.7 Things lost / different in Uno vs. MAUI / original

* **Desktop notifications are a no‑op.** The reflection still loads, the share sheet on Windows still works (via `DataTransferManager`), but the daily reminder feature simply does not run on `net10.0-desktop`. Linux/macOS toast notifications are not implemented.
* **No version tracking / preference migration.** `VersionTracking.Track()` and the two `*IfNeeded` migrations from `App.OnStart` did not survive the port. If the Uno head ships and the embedded database is later updated, there is no version gate to call `IDailyReflectionDatabase.RefreshDatabaseFile()`.
* **HTML rendering downgraded to text.** Where MAUI/Xamarin used `Label TextType="Html"` to render `<i>…</i>` inline, the Uno port strips all tags via `HtmlToTextConverter` and applies italic at the `TextBlock` level. The Reflection's `Title` and `Thought` fields therefore lose any inline emphasis (they are not currently italicised). This is the only feature where the visible UI differs from the original.
* **`TableView` replaced by hand‑rolled cards.** The Settings page no longer matches the OS‑native settings look the original got from `TableView Intent="Settings"`; visually it's a `Grid`/`StackPanel` mosaic. Functionally it is equivalent.
* **No `EventToCommandBehavior`.** The MAUI implementation routed `DateChangedEventArgs` to a command via `mct:EventToCommandBehavior` + `DateChangedEventArgsConverter`. The Uno port wires the `DatePickerFlyout.DatePicked` handler in code‑behind and updates the VM's `Date` directly. `DateChangedEventArgsConverter` exists in the converter folder but is no longer referenced by XAML.
* **Pages registered as Transient** rather than Singleton (MAUI's `AddPages()` uses Singleton). Combined with the `Visibility` region navigator this is fine because the views materialise once per region, but it is a behavioural difference if any page accidentally relied on singleton state.
* **Auto‑registration narrowed.** `AddPages()` (the reflection scan over `Page` subclasses) is replaced with explicit `AddTransient<MainPage>()` etc. in `App.OnLaunched`. `AddAllSubclassesOf<ViewModelBase>` is still used.
* `prompt.md`'s instructions said "no visual redesign", but the Uno port unavoidably changed three things to compile: (1) HTML stripping, (2) `TableView` → grids, (3) toolbar items → `CommandBar`. None of these are reversible without bespoke control work.

---

## 6. UI API mapping (side‑by‑side)

| Original (Xamarin.Forms) | MAUI | Uno Platform | Notes |
|---|---|---|---|
| `Shell` + `<TabBar>` | `Shell` + `<TabBar>` | `Page` (`MainPage`) hosting `utu:TabBar` + `Uno.Extensions.Navigation` regions | Visibility navigator keeps tabs in memory like Shell. |
| `ContentPage` | `ContentPage` | `Page` | Same XAML idea, different namespace. |
| `ContentPage.ToolbarItems` | same | `CommandBar` + `AppBarButton`(+ `CommandBar.SecondaryCommands`) | Page title moves into `CommandBar.Content`. |
| `TableView` (`Intent="Settings"`) + `SwitchCell` / `TextCell` | same | Hand‑rolled `Grid`s + `ToggleSwitch` / labels | No WinUI/Uno equivalent. |
| `Picker` | same | `ComboBox` | Bound to enum list directly. |
| `DatePicker` (`CustomDatePicker` subclass) | same + handler `Mapper` customisation | `CalendarDatePicker` and/or `DatePickerFlyout` | Uno picks the modal flyout for the toolbar UX. |
| `TimePicker` | same | `TimePicker` (WinUI) | No subclass needed. |
| `ActivityIndicator` | same | `ProgressRing` | |
| `ScrollView` / `StackLayout` / `Label` | same | `ScrollViewer` / `StackPanel` / `TextBlock` | |
| `Label TextType="Html"` | same | `TextBlock` + `HtmlToTextConverter` | Loses inline `<i>`. |
| `xct:EventToCommandBehavior` | `mct:EventToCommandBehavior` | (none) — code‑behind handler updates VM | |
| `FontImage` resource (Xamarin) | same | `FontIcon` element + `FontFamily` static resource | |
| `OnPlatform` colour overrides | same | `ThemeResource` + theme dictionaries | |
| App theme via `AppThemeBinding` | same | `ThemeResource` + WinUI theme dictionaries | Light/dark behaviour preserved. |
| Custom renderers (`CustomDatePickerRenderer`) | Handler `Mapper.AppendToMapping` | (not needed) | |

## 7. Non‑UI API mapping (side‑by‑side)

| Concern | Xamarin.Forms (original) | MAUI | Uno Platform |
|---|---|---|---|
| Preferences | `Xamarin.Essentials.IPreferences` (DI‑injected). | `Microsoft.Maui.Storage.Preferences.Default`. | `Windows.Storage.ApplicationData.Current.LocalSettings` (+ Android `SharedPreferences` mirror). |
| Share | `Xamarin.Essentials.IShare.RequestAsync(ShareTextRequest)`. | `Microsoft.Maui.ApplicationModel.DataTransfer.Share.Default.RequestAsync(...)`. | `Windows.ApplicationModel.DataTransfer.DataTransferManager.ShowShareUI()` + `DataRequested` event. |
| Version tracking | `Xamarin.Essentials.VersionTracking.Track()`. | `Microsoft.Maui.ApplicationModel.VersionTracking.Track()`. | **Missing.** |
| Settings migrations | `MigrateSettingsIfNeeded` + `RefreshDatabaseIfNeeded` in `App.OnStart`. | Same logic, same triggers. | **Missing.** |
| Permissions | `Xamarin.Essentials.Permissions` + custom `BasePlatformPermission` for Android 13+. | `Permissions.PostNotifications` (built in). | Equivalent Android permission helpers + `POST_NOTIFICATIONS` runtime check. |
| Notifications (Android) | `AlarmManager` + `PendingIntent` + `DailyNotificationReceiver` + `WakeUpAlarmReceiver`. | Same. | Same, plus `SCHEDULE_EXACT_ALARM` / `USE_EXACT_ALARM` / `WAKE_LOCK` permissions. |
| Notifications (iOS) | `UNUserNotificationCenter` + `UNCalendarNotificationTrigger` (daily repeat). | Same. | Same. |
| Notifications (desktop) | n/a | n/a | **No‑op.** |
| Database | `sqlite-net-pcl` 1.7.335, embedded `dailyreflections.db`, extracted to `LocalApplicationData` on first run, opened `ReadOnly`. | 1.10.196‑beta, otherwise identical. | Identical to MAUI, plus `SQLitePCLRaw.bundle_e_sqlite3` 2.1.10 explicitly referenced. |
| Date math | NodaTime `Period.Between` for sober period. | Same. | Same. |
| MVVM | `Microsoft.Toolkit.Mvvm` 7.0‑preview4 (`ObservableRecipient`, manual `[Property]`). | `CommunityToolkit.Mvvm` 8.4.0 (`[ObservableProperty]` + `[RelayCommand]` source generators). | Same as MAUI. |
| Messaging | `WeakReferenceMessenger` (3 messages). | Same. | Same. |
| DI | `Microsoft.Extensions.Hosting`. | `MauiAppBuilder`. | `Uno.Extensions.Hosting` `IHostBuilder`. |
| Configuration | `EmbeddedFileProvider` over `appsettings.json`. | `ConfigurationBuilder().AddJsonStream(manifestStream)`. | `UseConfiguration(c => c.EmbeddedSource<App>())`. |

---

## 8. Gaps and new features per migration

### 8.1 MAUI port

**Gaps vs. original:** none of substance. Behaviour parity is preserved.

**Behavioural changes to be aware of:**

* `IPreferences` / `IShare` are no longer registered in DI. Tests that previously mocked them must now mock `ISettingsService` / `IShareService` instead. The shared `DailyReflection.Presentation.Tests` already does this, so the existing test suite still applies, but the static `Preferences.Default` / `Share.Default` callers in the head are not unit‑testable without a MAUI host.
* macCatalyst is a new platform target; nothing currently differentiates it from iOS at runtime, but it inherits all of iOS's behaviour through shared partials.
* Custom renderers are gone. Picker chrome lives in `App.xaml.cs` handler customisation. If you previously relied on `OnElementChanged` overrides, port them to `Mapper.AppendToMapping`.

**Net‑new features:**

* macCatalyst target.
* MAUI single‑project resource pipeline (`Resources/AppIcon`, `Resources/Splash`, `Resources/Fonts`, `Resources/Images`).
* MAUI Community Toolkit `EventToCommandBehavior` (replacing the Xamarin Community Toolkit one).
* Default MAUI palette (Primary/Secondary/Tertiary/Gray100‑Gray950) merged on top of the original blue.

### 8.2 Uno Platform port

**Gaps vs. MAUI / original:**

1. **Desktop notifications** — `NotificationService.cs` (un‑guarded) is a no‑op. The Settings UI on desktop will toggle, but no reminder will ever fire. This is the biggest open item.
2. **Version tracking + preference / database migrations** — neither `VersionTracking.Track()` nor the `MigrateSettingsIfNeeded` / `RefreshDatabaseIfNeeded` calls are present. Any future schema bump will need this re‑plumbed.
3. **HTML rendering** — `<i>…</i>` markup in the database is stripped via `HtmlToTextConverter`. Reading is italicised at the control level, but inline emphasis in the Title/Thought fields is lost. WinUI has no built‑in HTML label; closing the gap means a custom `RichTextBlock` parser or storing pre‑italicised plain text in the DB.
4. **`TableView` look** — the Settings page is a hand‑built card list; it does not match the OS‑native settings appearance the original got for free on iOS / Android.
5. **macCatalyst target** — not produced by the Uno port. macOS desktop is reachable via `net10.0-desktop` (Skia), but that does not run as a Catalyst app.
6. **`EventToCommandBehavior`** on `DatePickerFlyout` — replaced with a `DatePicked` handler in code‑behind. Functional, but adds a code‑behind dependency that the original avoided.

**Behavioural changes to be aware of:**

* Pages are `AddTransient` rather than Singleton. Region navigation still keeps the materialised view alive, but the registration semantics differ from MAUI.
* `DateTime` settings round‑trip through `DateTime.ToBinary()` because of `LocalSettings` constraints. This is fine on its own; it does mean preferences written by the Uno port are not byte‑for‑byte compatible with the MAUI port's `Preferences.Default` storage on the same device.
* On Android, settings are mirrored to a `SharedPreferences` file named `DR_Settings` (the constant is shared via `PreferenceConstants.PreferenceSharedName`) so that the broadcast receivers can read them off the UI thread / outside the app process.
* `AndroidManifest.xml` carries three additional permissions (`SCHEDULE_EXACT_ALARM`, `USE_EXACT_ALARM`, `WAKE_LOCK`) that MAUI / Xamarin did not declare.

**Net‑new features:**

* Desktop target (`net10.0-desktop` running on Win32 / macOS / X11 / Linux framebuffer).
* Skia rendering pipeline (consistent visual behaviour across all targets that use it).
* `Uno.Toolkit.UI` controls (`TabBar`, `SafeArea`).
* `Uno.Extensions.Hosting` + `Uno.Extensions.Navigation` + `Uno.Extensions.Configuration` stack — the same kind of generic‑host wiring the Xamarin original used, but native to Uno.
* Compiled `x:Bind` bindings throughout, with helper methods for inline formatting.
* Three new converters (`NullToBoolConverter`, `BoolToVisibilityConverter`, `AllFalseBoolToVisibilityConverter`) and `HtmlToTextConverter` for stripping DB markup.
* Localisation scaffolding (`Strings/en/Resources.resw`).
* `Uno.Resizetizer` SVG icon/splash pipeline.

---

## 9. Recommendations / outstanding work

The items below are not asks of this report; they are the concrete deltas that would bring each port to full parity with the original.

**Uno port — to reach feature parity with MAUI:**

1. Implement desktop notifications (Windows toast notifications via `Microsoft.Toolkit.Uwp.Notifications` or platform‑specific schedulers; macOS via `NSUserNotification`; Linux via `libnotify`). At minimum, surface a clear "notifications not supported on this platform" message on desktop targets where the gap will remain.
2. Re‑introduce `VersionTracking.Track()` + the two `*IfNeeded` migrations in `App.OnLaunched`. Uno does not expose the MAUI/Essentials `VersionTracking` directly, so this likely needs a small wrapper persisting build/version into `LocalSettings`.
3. Decide whether HTML emphasis in the DB matters; if it does, replace `HtmlToTextConverter` + `TextBlock` with a `RichTextBlock` and a small inline‑markup parser.
4. Move the `DatePickerFlyout.DatePicked` wiring from code‑behind onto a behaviour or `EventToCommand` from `Uno.Toolkit.UI` to match the original's pure‑XAML pattern.

**MAUI port — opportunistic improvements:**

1. The macCatalyst head currently inherits iOS verbatim; consider adding a Catalyst‑only window size hint or menu wiring to make it feel like a Mac app rather than an iPad app on macOS.
2. The `Visual="Material"` attribute on `AppShell.xaml` is a no‑op in MAUI; either remove it or replace with explicit Material 3 theming if that is the desired look.
3. Consider re‑introducing `IPreferences` / `IShare` as DI‑registered abstractions (or using the Uno port's split between cross‑platform interface in `Services` and per‑head implementation) so that the head's `SettingsService` / `ShareService` are unit‑testable again.

---

## 10. Deep gap analysis — Uno port vs. original Xamarin.Forms app

This section is a focused, file‑level diff between the **original Xamarin.Forms app** (`heads/DailyReflection.Xamarin/…`) and the **Uno port** (`heads/DailyReflection.Uno/…`). It supersedes the higher‑level §5/§8 by going through every page, service, manifest, and lifecycle hook concretely. Items are grouped by severity:

* 🔴 **Functional gap** — a feature, behaviour or guarantee from the Xamarin app is missing or broken on the Uno port.
* 🟠 **Behavioural drift** — the feature still works but behaves differently (different data, identifiers, wording, persistence, etc.).
* 🟡 **Quality / parity issue** — code‑style, testability, or porting cleanliness gaps that don't break the user experience but should be closed.

### 10.1 Daily Reflection page

Reference files:
* Xamarin: `Views/DailyReflectionView.xaml(.cs)`
* Uno: `Views/DailyReflectionPage.xaml(.cs)`

| # | Severity | Gap | Detail |
|---|---|---|---|
| 10.1.1 | 🔴 | **HTML inline emphasis is lost.** | The DB stores the *Reading* with `<i>…</i>` tags and the copyright string as `From the book <i>Daily Reflections</i><br>…`. Xamarin renders these via `Label TextType="Html"`. The Uno port pipes everything through `HtmlToTextConverter` (`StringExtensions.StripHtml`) and applies a single `FontStyle="Italic"` at the `TextBlock` level. So the *Title* and *Thought* fields lose any inline italics they may carry, the `<br>` in the copyright line collapses, and any future markup added to the DB will silently disappear. Closing the gap requires either a `RichTextBlock` with an inline parser or pre‑processing the DB strings into `Run`s. |
| 10.1.2 | 🔴 | **Loading & empty/error logic is not equivalent.** | Xamarin combines `HasError` and `GetReflectionCommand.IsRunning` via `AllFalseMultiConverter` so that the content stack hides while the spinner shows. Uno's `ShowContent(bool, bool)` helper returns `Visible` only when both are false — fine — but the `ProgressRing` is layered *under* the StackPanel inside the same `Grid`, while in Xamarin the `ActivityIndicator` is the last child of the `Grid` (so it z‑orders on top). Net effect: on Uno the `ProgressRing` is partially hidden behind whatever content was previously rendered; on Xamarin it sits centred above. |
| 10.1.3 | 🟠 | **The "first label" iOS workaround is gone.** | Xamarin places an empty `Label TextType="Html"` with `HeightRequest=0` as the first child on iOS only (commented "first element with TextType='Html' doesn't get rendered on first load for some reason"). Because Uno strips HTML entirely, the workaround is moot — but worth flagging in case a future Uno port re‑adds HTML rendering, the same iOS first‑load bug may resurface. |
| 10.1.4 | 🟠 | **Initial load semantics differ.** | Xamarin: `OnAppearing` calls `vm.Init()` via `MainThread.BeginInvokeOnMainThread`, and `Init` guards via `_initialized` so the call is once‑per‑VM. Uno: `Loaded` fires `GetDailyReflectionCommand.ExecuteAsync(null)` directly with no idempotency guard — every time the page is materialised the command re‑runs. Combined with 10.1.5, this means navigating away and back re‑hits the database. `vm.Init()` is never called on the Uno port at all. |
| 10.1.5 | 🟠 | **Page lifetime differs.** | Xamarin's Shell holds one `DailyReflectionView`; `OnAppearing` fires per visit. Uno registers `DailyReflectionPage` as `Transient` (App.xaml.cs line 56) and uses `Region.Navigator="Visibility"` — depending on how the region navigator caches, the page may be either kept hidden (singleton‑ish) or recreated. Either way, `Loaded` is *not* the same event boundary as `OnAppearing`. |
| 10.1.6 | 🟠 | **Toolbar / title placement.** | Xamarin sets `Title="{Binding Date, StringFormat='{0:MMMM d}'}"` on the page so Shell renders it as the navigation‑bar title. Uno injects the formatted date into `CommandBar.Content` instead — visually the title sits inside the toolbar rather than as a navigation title. |
| 10.1.7 | 🟠 | **`EventToCommand` replaced with code‑behind.** | The `DateChangedEventArgsConverter` in `/Converters` is unused; Uno's `DatePickerFlyout_DatePicked` mutates the VM directly. Functionally equivalent, but moves logic out of XAML. |
| 10.1.8 | 🟡 | **Automation IDs are not applied.** | `DailyReflectionView.xaml` decorates every label and toolbar item with `AutomationId="{x:Static constants:AutomationConstants.DR_*}"`. The shared `DailyReflection.Core/Constants/AutomationConstants.cs` still ships those constants, but **no Uno page references them**. UI tests written against `DR_Reflection_Title`, `DR_Share_Reflection`, `DR_Change_Date`, etc. will not work against the Uno build. The WinUI equivalent is `AutomationProperties.AutomationId="..."`. |
| 10.1.9 | 🟡 | **Custom date picker is gone.** | Xamarin uses a `<controls:CustomDatePicker IsVisible="False">` driven by `EventToCommandBehavior` so the *Title‑bar* date format and behaviour stay consistent across Android/iOS via `CustomDatePickerRenderer`. Uno replaces this with `DatePickerFlyout` (modal). Visually different (a popup dialog vs. a focused inline picker) and not customised per‑platform. |
| 10.1.10 | 🟡 | **Page `Resources` dictionary contains a stale entry.** | `DailyReflectionPage.xaml` declares `<converters:PluralityConverter x:Key="YearsPluralityConverter">` even though it is never bound on this page; that resource only made sense on `SobrietyTimePage`. Likely a copy‑paste leftover. |

### 10.2 Sober Time page

Reference files:
* Xamarin: `Views/SobrietyTimeView.xaml(.cs)`
* Uno: `Views/SobrietyTimePage.xaml(.cs)`

| # | Severity | Gap | Detail |
|---|---|---|---|
| 10.2.1 | 🔴 | **`SoberDate` null check uses the wrong converter on Uno.** | Xamarin uses `Xamarin.CommunityToolkit.IsNotNullOrEmptyConverter` (registered in App.xaml as `NullToBoolConverter`). Uno's `NullToBoolConverter` (`Converters/NullToBoolConverter.cs`) returns `Visibility.Visible` when the value is *not* null — that part is fine — but `Visibility="{x:Bind ViewModel.SoberDate, Mode=OneWay, Converter=...}"` cannot fire on an x:Bind of a `DateTime?` property the same way `OneWay` Binding did when only the value (not "has value") changes. After `SettingsViewModel` writes a new `SoberDate`, Uno's `SobrietyTimeViewModel.SoberDate` setter does fire, so the binding *will* re‑evaluate, but only because of `[ObservableProperty]` raising `OnPropertyChanged(nameof(SoberDate))`. Worth confirming with a runtime test: change preference → does the calendar block become visible without restart? |
| 10.2.2 | 🟠 | **No `OnAppearing` activation hook.** | Xamarin's `SobrietyTimeView.OnAppearing` sets `vm.IsActive = true` so the messenger registrations come back online when the user re‑enters the tab. Uno's `SobrietyTimePage` sets `IsActive = true` once in the constructor and never resets. With a Transient page that's fine, but if the page is ever cached / region navigated, the messenger handlers might double‑register or never re‑activate. |
| 10.2.3 | 🟠 | **Layout grid changed shape.** | Xamarin's outer grid is `RowDefinitions="20, Auto, *, Auto, Auto, *, Auto"` (top spacer + flexible pushers). Uno keeps the same row template, but inserts a separate `TextBlock Grid.Row="0"` with the page title (because Uno has no Shell title) — that title eats vertical space that didn't exist on Xamarin, so the visual centring of the date+period block is shifted. |
| 10.2.4 | 🟡 | **Automation IDs missing.** | `Sobriety_Time` and `ST_One_Day_At_A_Time` are still defined in `AutomationConstants`. Neither shows up in `SobrietyTimePage.xaml`. |
| 10.2.5 | 🟡 | **`DisplayPreferenceString` instead of `x:Static`.** | Xamarin: `IsDaysMonthsYearsDisplayPreferenceConverter` is parameterised with `DisplayPreference="{x:Static models:SoberTimeDisplayPreference.DaysMonthsYears}"` — type‑safe enum reference. Uno passes the **string** `"DaysMonthsYears"` and parses it back inside the converter. A typo here goes undetected at compile time. |
| 10.2.6 | 🟡 | **Empty `OnUnloaded` handler.** | `SobrietyTimePage.xaml.cs` registers `Unloaded += OnUnloaded` and `OnUnloaded` is empty. Either set `IsActive = false` here (matching Xamarin's `OnDisappearing` semantics) or remove the dead handler. |

### 10.3 Settings page

Reference files:
* Xamarin: `Views/SettingsView.xaml(.cs)`
* Uno: `Views/SettingsPage.xaml(.cs)`

| # | Severity | Gap | Detail |
|---|---|---|---|
| 10.3.1 | 🔴 | **No `OnDisappearing` deactivation.** | Xamarin's `SettingsView` flips `vm.IsActive = false` on `OnDisappearing`; Uno never deactivates. `WeakReferenceMessenger` recipients keep accumulating because `Register` is called once in the constructor (line 31) but the matching `UnregisterAll` is never invoked. The page is Transient, so multiple navigations create multiple instances all subscribed to `NotificationPermissionRequestMessage`. The first reply wins (`AsyncRequestMessage<bool>`), but every additional instance leaks until GC. |
| 10.3.2 | 🔴 | **Permission‑dialog "OK" no longer routes the user to Settings.** | Xamarin's flow: `DisplayAlert` returns `bool`, the VM uses that result via `m.Reply(...)`, and on `true` the VM calls `INotificationService.ShowNotificationSettings()`. Uno's `ShowPermissionDialogAsync` returns `result == ContentDialogResult.Primary` correctly — but on the desktop target `INotificationService.ShowNotificationSettings()` is a **no‑op** (`PlatformServices/NotificationService.cs` line 24). So if a user has notifications denied on Windows/Linux/macOS, hits "OK", the dialog closes and *nothing else happens*. |
| 10.3.3 | 🔴 | **`TableView Intent="Settings"` look is gone.** | The original used MAUI/Xamarin's native settings table look (grouped sections with iOS chrome / Android Material). Uno re‑builds it as plain `Grid` cards. This is a known consequence of WinUI not having `TableView`, but it is a visual regression vs. the source app. |
| 10.3.4 | 🟠 | **`NotificationTime` rebases the date to today.** | Xamarin stores the picker time as a `TimeSpan` (via `DateTimeToTimeSpanConverter`) and writes it back into the VM's `DateTime` preserving the original date component (the existing stored DateTime). Uno's `NotificationTimePicker_TimeChanged` does `ViewModel.NotificationTime = DateTime.Today.Add(e.NewTime)`. Net effect: the DateTime stored in preferences flips to today's date every time the user touches the time picker. The shared `SettingsViewModel.NotificationTime` setter pushes that to `SettingsService`, which on Android mirrors it as `DateTime.ToBinary()` to `SharedPreferences`. The receiver code reads it back via `DateTime.FromBinary(timePref)` and uses only `.TimeOfDay`, so functionally it still works — but the persisted DateTime is no longer comparable across days, which subtly contradicts the migration semantics in `App.MigrateSettingsIfNeeded` if it ever runs on a value written by the Uno port. |
| 10.3.5 | 🟠 | **Notification time tap is gated by `NotificationsEnabled` differently.** | Xamarin: `TextCell IsEnabled="{Binding NotificationsEnabled}"` plus `TimePickerLabelEnabledConverter` which **always returns true on Android** (so the time label remains tappable on Android even when notifications are off — Xamarin's intentional UX choice). Uno: the wrapping `Grid` has `IsEnabled="{x:Bind ViewModel.NotificationsEnabled, Mode=OneWay}"` and additionally `NotificationTime_Tapped` early‑returns if `ViewModel.NotificationsEnabled` is false. Net effect: on Uno's Android target the row is greyed out and untappable, contradicting the Xamarin design. `TimePickerLabelEnabledConverter` exists in Xamarin but **was not ported to Uno** at all. |
| 10.3.6 | 🟠 | **`MaxDate` type mismatch.** | The shared `SettingsViewModel.MaxDate` is a `DateTimeOffset`. Xamarin's `CustomDatePicker` binds `MaximumDate` (a `DateTime`) so Forms's binding engine performs the conversion. Uno binds `CalendarDatePicker.MaxDate` which is a `DateTimeOffset` — fine — but compiled `x:Bind` is strict: if anyone changes the VM property type to `DateTime`, the Uno page fails to compile while the Xamarin page would still work via the BindingValueConverter. Inverse risk: if the binding converter ever becomes lossy across local/utc boundary, the picker may drift by one day around midnight. |
| 10.3.7 | 🟠 | **App version is hard‑coded.** | `SettingsPage.xaml.cs` exposes `AppVersion => VersionConstants.VersionNumber` (literal string `"1.0.0"`). Xamarin reads `VersionTracking.CurrentVersion` from `Xamarin.Essentials`. The Uno value will never reflect the actual installed build. |
| 10.3.8 | 🟡 | **`SoberTimeDisplay_Tapped` event handler in code‑behind.** | All three rows on Settings now have tap event handlers + visibility toggles. Xamarin had the same pattern. Net result: not strictly worse, but the Uno code grows three event handlers + three picker controls hidden in the tree. Consider using `Flyout` similar to the Daily Reflection toolbar for cleaner UX. |
| 10.3.9 | 🟡 | **No automation IDs.** | `Settings`, `Settings_Enable_Notif_Switch`, `Settings_Time_Picker`, `Settings_Date_Picker`, `Settings_Notification_Time`, `Settings_Sober_Date` — none referenced in `SettingsPage.xaml`. |

### 10.4 Navigation shell

Reference files:
* Xamarin: `Views/AppShell.xaml(.cs)`
* Uno: `Views/MainPage.xaml(.cs)` + `App.xaml.cs` (`RegisterRoutes`)

| # | Severity | Gap | Detail |
|---|---|---|---|
| 10.4.1 | 🟠 | **Tab icon font‑family mismatch.** | Xamarin renders the `ClockIcon` from `FaRegularFont`. Uno's `MainPage.xaml` uses `FontAwesomeRegular` for the *clock* glyph (correct) but uses `FontAwesomeSolid` for the *book* (Reflection) glyph and *gear* (Settings) glyph — same as Xamarin's `ReflectionIcon`/`SettingsIcon`, which were `FaSolidFont`. So tab icons match. **However** `Uno's MainPage` uses `Content="Reflection"` / `"Sober Time"` / `"Settings"` for the tab labels — Xamarin's Shell rendered the `Title` attribute. Uno labels are correct, but `TabBarItem.Content` is rendered both as visible label *and* as accessible name; Xamarin's Shell rendered the title as a separate, navigation‑bar text. |
| 10.4.2 | 🟠 | **Shell theming lost.** | Xamarin's `App.xaml` registers a `BaseStyle` targeting `Element` with `ApplyToDerivedTypes="True"` that sets `Shell.BackgroundColor`, `Shell.ForegroundColor`, `Shell.TabBarBackgroundColor`, `Shell.NavBarHasShadow`, `Shell.UnselectedColor`, `Shell.TabBarTitleColor`. Several of these values use `OnPlatform` (`#1976D2` on Android vs. `BackgroundColorLight` on iOS for Shell background; `iOSSystemBlue` for foreground) so the iOS app gets light navbar / blue text, the Android app gets material blue navbar / white text. **None of this lives in the Uno port.** The Uno `TabBar` uses default Uno.Toolkit theming — it neither matches the Android Material blue navbar nor the iOS light navbar. The "primary blue" `#1976D2` is preserved as a colour resource but is never wired into the navigation chrome. |
| 10.4.3 | 🟠 | **Page background colour rule lost.** | Xamarin's `App.xaml` has `<Style TargetType="Page" ApplyToDerivedTypes="True">` setting `BackgroundColor="{AppThemeBinding Dark={StaticResource BackgroundColorDark}, Light={StaticResource BackgroundColorLight}}"`. Uno pages set `Background="{ThemeResource ApplicationPageBackgroundThemeBrush}"`, which is the WinUI default (close to `#FFFFFF`/`#000000`), not the original `#EFF2F5`/`#121212`. |
| 10.4.4 | 🟠 | **Label default text colour lost.** | `<Style TargetType="Label">` sets `TextColor` to `TextPrimaryColorDark`/`TextPrimaryColorLight`. Uno's `TextBlock`s default to WinUI's `TextFillColorPrimaryBrush`, which is close (FFE4E4E4 / dark, 1F1F1F / light) but not pixel‑identical. |
| 10.4.5 | 🟡 | **`Shell.TabBarTitleColor = #1976D2` not replicated.** | Selected tab title was deliberately blue. Uno uses Toolkit defaults. |

### 10.5 Notifications

Reference files:
* Xamarin: `DailyReflection.iOS/Services/NotificationService.cs`, `DailyReflection.Android/Services/NotificationService.cs`, `DailyReflection.Android/BroadcastReceivers/*`, `Permissions/NotificationPermission.cs`.
* Uno: `PlatformServices/NotificationService.{cs,iOS.cs,Android.cs}`, `Platforms/Android/BroadcastReceivers/*`.

| # | Severity | Gap | Detail |
|---|---|---|---|
| 10.5.1 | 🔴 | **Desktop notifications: hard no‑op.** | `PlatformServices/NotificationService.cs` returns `false`/`Task.FromResult(false)` from `CanScheduleNotifications` and `TryScheduleDailyNotification`, and is empty for `CancelNotifications`/`ShowNotificationSettings`. The Settings UI on Windows / macOS desktop / Linux toggles `NotificationsEnabled = true`, persists it, but no toast ever fires. There is also no UI affordance telling the user it's a no‑op — they will believe notifications are on. |
| 10.5.2 | 🔴 | **iOS Sound channel.** | Xamarin's iOS `UNMutableNotificationContent` does **not** set `Sound`. Uno sets `Sound = UNNotificationSound.Default`. Ostensibly an *improvement*, but it is a behaviour change: any user who silenced the original app via the system per‑app sound setting will be surprised. |
| 10.5.3 | 🔴 | **iOS authorisation options widened.** | Xamarin requests `UNAuthorizationOptions.Alert`. Uno requests `Alert | Badge | Sound`. Apple's permission dialog is "all‑or‑nothing" so this means new installs of the Uno port will be asked for badge + sound on top of alert, where the original only asked for alert. Existing users on the original who upgrade to the Uno binary will not be prompted again (their `Authorized` state covers the wider set). |
| 10.5.4 | 🟠 | **iOS notification identifier.** | Xamarin uses `MessageId.ToString()` where `MessageId = 1` → identifier `"1"`. Uno uses `"DailyReflectionNotification"`. Different ids, so a user who had the Xamarin app installed and updates to the Uno build will see the old identifier orphaned (until `RemoveAllPendingNotificationRequests` runs at the next schedule, which Uno does — so safe in practice; just worth documenting if a side‑by‑side install is ever tried). |
| 10.5.5 | 🟠 | **iOS `CancelNotifications` is more aggressive on Uno.** | Xamarin: `RemoveAllPendingNotificationRequests`. Uno: `RemoveAllPendingNotificationRequests` + `RemoveAllDeliveredNotifications` + `ApplicationIconBadgeNumber = 0`. Different observable behaviour: opening the Uno app silently dismisses any delivered (still on lock screen) reflection notifications and wipes the app icon badge. Probably desirable, but again a deliberate behaviour change vs. the original. |
| 10.5.6 | 🟠 | **iOS `Subtitle` dropped.** | Xamarin sets `Subtitle = ""`. Uno omits the property entirely. Visually identical (empty subtitle vs. unset both render no subtitle row), but worth a sanity check on iPad where subtitles can render. |
| 10.5.7 | 🟠 | **Android notification title.** | Xamarin: `SetContentTitle("Time for the daily reflection!")` only — no body. Uno: `SetContentTitle("Daily Reflection")` + `SetContentText("Time for the daily reflection!")`. Different layout in the notification shade. |
| 10.5.8 | 🟠 | **Android `Priority` was unset on Xamarin.** | Uno calls `SetPriority(NotificationCompat.PriorityDefault)`; Xamarin never set a priority at all. Effectively the same default but explicit on Uno. |
| 10.5.9 | 🟠 | **Android receiver `Exported` flag.** | Xamarin's `DailyNotificationReceiver` is `[BroadcastReceiver(Enabled = true)]` (no `Exported`). Uno is `[BroadcastReceiver(Enabled = true, Exported = false)]`. `Exported=false` is correct for an internal alarm receiver and is more secure; just a behaviour delta. |
| 10.5.10 | 🟠 | **Android pending intent flags differ.** | Xamarin: `ActivityFlags.ClearTop`. Uno: `ActivityFlags.ClearTop \| ActivityFlags.SingleTop`. Uno's choice prevents stacking multiple `MainActivity` instances when the user repeatedly taps reflection notifications without dismissing — better behaviour, but a delta. |
| 10.5.11 | 🟠 | **Android permission API.** | Xamarin uses `Xamarin.Essentials.Permissions.CheckStatusAsync<NotificationPermission>()` with a custom `NotificationPermission : BasePlatformPermission` that lists `POST_NOTIFICATIONS` only on API 33+. Uno uses `Uno.Extensions`'s `PermissionsHelper.CheckPermission(Android.Manifest.Permission.PostNotifications, ...)`. Functionally equivalent on Tiramisu+; on older API levels Xamarin's permission list is empty (always granted), Uno hard‑codes `return true`. Same result, different code path. |
| 10.5.12 | 🟠 | **Android receiver re‑schedule path mutates state on the singleton.** | Both implementations new‑up a `NotificationService()` from inside the receiver and call `TryScheduleDailyNotification(..., shouldRequestPermission: false)`. Uno wraps this in a `try { … } catch { /* silently fail */ }` — Xamarin does not. Uno is more defensive, but silently swallowing exceptions hides bugs. Consider logging. |
| 10.5.13 | 🟠 | **`channel.SetSound()` not called.** | Neither port calls `NotificationChannel.SetSound`; Xamarin gets away with it because each posted notification calls `SetSound(RingtoneManager.GetDefaultUri(...))`. Uno does the same — fine on both — but on Android 8+ the system officially prefers channel‑level sound. Latent issue carried over. |
| 10.5.14 | 🟡 | **Three new manifest permissions in Uno.** | Uno's `AndroidManifest.xml` declares `SCHEDULE_EXACT_ALARM`, `USE_EXACT_ALARM`, `WAKE_LOCK`. The Xamarin original declares **none** of these and still works because (a) `SetAndAllowWhileIdle` is best‑effort, (b) target SDK 33 means `SCHEDULE_EXACT_ALARM` is granted by default, and (c) wake locks are not used. The Uno declarations don't break anything but they do require Play Store policy answers (especially `USE_EXACT_ALARM` requires a calendar/alarm‑clock policy declaration). If the goal is to match the original's submission profile, **drop these three permissions** — the existing alarm code will continue to work without them. |
| 10.5.15 | 🟡 | **No `DailyReflection.Uno.Droid.MainActivity` reference resilience.** | The Uno receiver hard‑codes `typeof(DailyReflection.Uno.Droid.MainActivity)` for the launch intent. If `MainActivity.Android.cs` is renamed or moved, the receiver fails silently. Xamarin's receiver does the same with `typeof(MainActivity)` so this is even, but worth a comment. |

### 10.6 Settings persistence

Reference files:
* Xamarin: `DailyReflection.Services/Settings/SettingsService.cs` (cross‑platform, `IPreferences`).
* Uno: `PlatformServices/SettingsService.cs` + `SettingsService.Android.cs`.

| # | Severity | Gap | Detail |
|---|---|---|---|
| 10.6.1 | 🔴 | **No `MigrateOldPreferences` trigger.** | The shared `ISettingsService.MigrateOldPreferences` exists; Uno's implementation is `Get → Set` of the same three keys. But `App.OnLaunched` never calls it (and `VersionTracking.IsFirstLaunchForCurrentBuild` is not available without re‑plumbing). So the migration code is dead. If a user upgrades from a Xamarin‑era install (even hypothetically) the legacy `Xamarin.Essentials` preferences container won't be touched. |
| 10.6.2 | 🔴 | **Cross‑platform `MigrateOldPreferences` semantics differ.** | Xamarin's `SettingsService.MigrateOldPreferences()` reads from `Xamarin.Essentials.Preferences` (default container, not `DR_Settings`) and writes them into `DR_Settings`. Uno's implementation just round‑trips values inside `LocalSettings` — it never reads from any "old" container. So even if it were called, it would do nothing useful. |
| 10.6.3 | 🟠 | **`Get<T>` fallback path silently downgrades.** | If `LocalSettings` holds a value of the wrong runtime type, Uno tries `Convert.ChangeType(value, typeof(T))` and on exception returns `defaultValue`. The shared `ISettingsService` contract (Xamarin) raises an exception on type mismatch (because `IPreferences` is type‑specific). The Uno port is more permissive — easier to debug, but masks the kind of mismatch that the version‑tracking migration was designed to detect. |
| 10.6.4 | 🟠 | **`DateTime.ToBinary()` round‑trip stores `Kind`.** | Xamarin's `IPreferences.Set(key, DateTime)` (Android impl) stores ISO‑8601 strings via `XmlConvert.ToString(value, XmlDateTimeSerializationMode.RoundtripKind)` and reads back with `DateTimeKind.Local`. Uno stores `DateTime.ToBinary()` — also round‑trip‑safe — but a value written by the Xamarin app cannot be read by the Uno app and vice‑versa (different binary encodings). On a fresh install this is invisible; on a side‑by‑side replacement you would lose the user's settings. |
| 10.6.5 | 🟠 | **Android `SharedPreferences` mirror is *write‑only*.** | `SettingsService.Android.cs` mirrors `Set` to the `DR_Settings` SharedPreferences for the receivers, but `Get<T>` always reads from `LocalSettings` first. If the `LocalSettings` file is wiped (re‑install, app‑data clear) and the Android SharedPreferences survive (Backup Manager, etc.), the foreground app will *not* recover settings from the mirror — the receivers will keep firing notifications based on the stale SharedPreferences value, but the UI will show defaults. |
| 10.6.6 | 🟠 | **`null` handling.** | Xamarin's `SettingsService.Set<T>` does not have a special path for `null`. Uno's `MirrorSet<T>` calls `editor.Remove(key)` when value is null, but the corresponding `LocalSettings` write at line 67 stores the literal `null`. `LocalSettings.Values[key] = null` actually *removes* the key in WinUI semantics — the two stores converge — but the logic isn't symmetric and is worth a comment. |
| 10.6.7 | 🟡 | **Constructor reads `ApplicationData.Current.LocalSettings` eagerly.** | Calling `ApplicationData.Current` on a non‑packaged Linux/macOS framebuffer build can throw. Uno generally handles this, but the constructor offers no graceful fallback. |

### 10.7 Sharing

Reference files:
* Xamarin: `DailyReflection.Services/Share/ShareService.cs` (uses `Xamarin.Essentials.IShare`).
* Uno: `PlatformServices/ShareService.cs` (uses `DataTransferManager`).

| # | Severity | Gap | Detail |
|---|---|---|---|
| 10.7.1 | 🔴 | **Race condition on concurrent shares.** | Uno's `ShareService` keeps `_pendingTitle`/`_pendingBody` as instance fields. `ShareText` mutates them, calls `ShowShareUI` (sync), and then `-=` the handler in `finally`. If a second `ShareText` runs before the first's `DataRequested` fires (it can — `ShowShareUI` is async on some platforms), the second call's title/body overwrite the first. Xamarin's `IShare.RequestAsync(new ShareTextRequest(...))` is intrinsically value‑typed and free of this hazard. Lock the service or pass the values via the `DataRequested` closure. |
| 10.7.2 | 🔴 | **Silently does nothing on platforms where `DataTransferManager.IsSupported() == false`.** | Linux (X11) returns false from `IsSupported`. The reflection share button on Linux does nothing — no error, no fallback. Xamarin's `IShare` would have thrown `NotImplementedOnPlatform` (or similar). |
| 10.7.3 | 🟠 | **`GetForCurrentView()` on Skia desktop.** | `DataTransferManager.GetForCurrentView()` is a UWP/WinAppSDK API; on Skia desktop targets Uno provides a polyfill. Recent Uno versions (5.x+) prefer `IDataTransferManagerInterop` via `WinRT.Interop.WindowNative.GetWindowHandle(window)`. The current code may degrade on macOS/Linux desktop. Validate against Uno 6.x docs. |
| 10.7.4 | 🟡 | **`+=`/`-=` of `DataRequested` per call.** | The handler is hooked and unhooked on every share call. If `ShowShareUI` throws synchronously the handler is removed before `DataRequested` can fire. Move handler registration into the constructor. |

### 10.8 App lifecycle, DI, configuration

Reference files:
* Xamarin: `App.xaml.cs`, `Startup.cs`, `MainActivity.cs`, `AppDelegate.cs`.
* Uno: `App.xaml.cs`, `DependencyInjection/Dependencies.cs`, `Platforms/{Android,iOS,Desktop}/*`.

| # | Severity | Gap | Detail |
|---|---|---|---|
| 10.8.1 | 🔴 | **`VersionTracking.Track()` not called.** | Xamarin's `App` constructor calls `VersionTracking.Track()` so `IsFirstLaunchEver`, `IsFirstLaunchForCurrentVersion`, `IsFirstLaunchForCurrentBuild`, `CurrentBuild`, `PreviousBuild` are populated on every app start. Uno never calls it. There is no equivalent in `Uno.Extensions` (it lives in MAUI Essentials). All version‑gated code paths are therefore dead on the Uno port. |
| 10.8.2 | 🔴 | **`MigrateSettingsIfNeeded` not called.** | Logic that was the entire point of `SettingsService.MigrateOldPreferences` and the `NewSettingsVersion`/`NewSettingsBuild` constants. Without it, a hypothetical migration from the original Xamarin install to a future Uno install loses preferences. |
| 10.8.3 | 🔴 | **`RefreshDatabaseIfNeeded` not called.** | The shared `IDailyReflectionDatabase.RefreshDatabaseFile()` exists and works (it deletes the cached `dailyreflections.db` and re‑extracts it from embedded resources). The Xamarin original calls it once, gated on a build version bump (`RefreshDatabaseBuild = 32`). Uno never calls it, so any future content update to the embedded database will not be picked up by users on existing installs. |
| 10.8.4 | 🟠 | **Notification re‑schedule on settings migration is also gone.** | Xamarin's `MigrateSettingsIfNeeded` re‑schedules notifications post‑migration via `INotificationService.TryScheduleDailyNotification`. Without the migration trigger, this can't run. |
| 10.8.5 | 🟠 | **Page registration is Transient on Uno, Singleton on Xamarin.** | Xamarin's `services.AddPages()` uses `AddAllSubclassesOf<Page>(ServiceLifetime.Singleton)`. Uno explicitly registers each of the four pages as `AddTransient`. Combined with `Region.Navigator="Visibility"`, in practice each page is materialised once per region instance — but there's no longer a single resolved instance through DI, so anything that injects a `Page` directly would get a fresh one each time. (Currently nothing does, so safe.) |
| 10.8.6 | 🟠 | **Logging: console only.** | Xamarin uses `host.ConfigureLogging(builder => builder.AddConsole())`. Uno's `InitializeLogging` is wrapped in `#if DEBUG` and uses platform‑aware providers (WASM/iOS/console) — but the DI host's `LoggerFactory` is *not* connected back into `Uno.Extensions.LogExtensionPoint` outside DEBUG, so release builds produce no logs at all. Xamarin produced console logs in release. |
| 10.8.7 | 🟠 | **Configuration source.** | Xamarin: `EmbeddedFileProvider` over the assembly + `AddJsonFile("appsettings.json")`. Uno: `UseConfiguration(c => c.EmbeddedSource<App>())`. Equivalent in result, different in error mode (Uno's `EmbeddedSource` is silent if the file is missing; Xamarin throws). |
| 10.8.8 | 🟡 | **`App.GetService<T>()` static accessor.** | Uno introduces a static service locator (`App.GetService<DailyReflectionViewModel>()`) used by every page constructor. Xamarin used `Startup.ServiceProvider.GetService<T>()` for the same purpose, so there's no functional regression — but both are anti‑patterns vs. constructor injection from a `ViewMap`/`RouteMap`. Worth migrating to `IServiceProvider`‑injected views per Uno.Extensions Navigation guidance. |

### 10.9 Manifests, permissions, capabilities

Reference files:
* Xamarin: `DailyReflection.Android/Properties/AndroidManifest.xml`, `DailyReflection.iOS/Info.plist`.
* Uno: `Platforms/Android/AndroidManifest.xml`, `Platforms/iOS/Info.plist`, `Package.appxmanifest`.

| # | Severity | Gap | Detail |
|---|---|---|---|
| 10.9.1 | 🟠 | **Bundle/package id changed.** | Xamarin: `com.kazo0.dailyreflection`. Uno: `com.kazo0.dailyreflectionuno` (csproj `ApplicationId`). New install identity — they coexist on a device, but ratings/IAP/etc. don't transfer. |
| 10.9.2 | 🟠 | **iOS supported orientations changed.** | Xamarin iPhone: portrait + portrait‑upside‑down only. Uno iPhone: portrait + landscape‑left + landscape‑right (no upside‑down, plus landscape allowed). Different iPad orientations too — Xamarin allows portrait‑upside‑down + landscapes, Uno same. Net: Uno iPhone now rotates to landscape, which the original explicitly forbids. |
| 10.9.3 | 🟠 | **iOS minimum OS version moved.** | Xamarin: `MinimumOSVersion=9.0` in Info.plist. Uno: relies on the csproj `SupportedOSPlatformVersion` (15.0 by default for net10.0‑ios). Drops support for everything pre‑iOS 15. |
| 10.9.4 | 🟠 | **iOS launch storyboard removed.** | Xamarin: `UILaunchStoryboardName=LaunchScreen`. Uno: replaced with the SVG splash via `Uno.Resizetizer`. Different launch UX. |
| 10.9.5 | 🟠 | **iOS app fonts both register the same OTFs**, but Uno's `Info.plist` does **not** set `CFBundleIdentifier`, `CFBundleName`, `CFBundleDisplayName`, `CFBundleShortVersionString`, `CFBundleVersion` — those come from the csproj. Behaviour is the same; just worth knowing for tooling that scrapes Info.plist. |
| 10.9.6 | 🟠 | **Android `RECEIVE_BOOT_COMPLETED` is declared on both** but Uno additionally declares `SCHEDULE_EXACT_ALARM`, `USE_EXACT_ALARM`, `WAKE_LOCK` (see 10.5.14). Drop these to match the original. |
| 10.9.7 | 🟠 | **Android target/min SDK invisible in Uno manifest.** | Xamarin: `<uses-sdk android:minSdkVersion="21" android:targetSdkVersion="33"/>` is explicit in the manifest. Uno: relies on csproj `SupportedOSPlatformVersion`/`TargetPlatformVersion`. The runtime min/target may differ from the original 21/33 unless the csproj is verified. |
| 10.9.8 | 🟡 | **Windows/desktop capabilities.** | `Package.appxmanifest` declares `runFullTrust`. The original Xamarin app has no Windows manifest at all. Net new — fine. |

### 10.10 Resources, fonts, theming

| # | Severity | Gap | Detail |
|---|---|---|---|
| 10.10.1 | 🔴 | **`Visual="Material"` semantics absent.** | Xamarin's Shell + every page declares `Visual="Material"` and the project references `Xamarin.Forms.Visual.Material`. This applies Material Design renderers to all controls on Android specifically (Material `EditText`, `Button`, etc.). Uno on Android renders via Skia (or native depending on TFM) and has no "make‑Android‑look‑like‑Material" toggle of the same scope. Visual difference on Android: text inputs and buttons render with WinUI's Fluent default, not Material. |
| 10.10.2 | 🟠 | **Original colour palette only partially applied.** | `Colors.xaml` brings over `Primary=#1976D2`, `BackgroundColorDark/Light`, `TextPrimaryColor*`, plus the WinUI default `Gray100..Gray950`/`MidnightBlue`/`OffBlack` palette. Original Xamarin had a leaner palette focused on the blue + 2 backgrounds + 2 text colours. The Uno port hasn't *removed* the originals, but they no longer drive the Shell or Page chrome (see 10.4.2 / 10.4.3). |
| 10.10.3 | 🟠 | **Font glyph keys renamed.** | Xamarin: `<FontImage x:Key="ReflectionIcon|SettingsIcon|ShareIcon|ClockIcon|CalendarIcon">`. Uno: replaced with `<x:String x:Key="Calendar|Book|Gear|Clock|Share">` (raw glyph strings) + per‑use `<FontIcon FontFamily=… Glyph=…>`. Equivalent functionally. The `*Icon` keys are removed, so any future XAML referencing `{StaticResource ShareIcon}` will fail to compile. |
| 10.10.4 | 🟠 | **`AppThemeBinding` replaced inconsistently.** | Where Xamarin used `{AppThemeBinding Dark=…, Light=…}`, Uno uses `{ThemeResource Foo}` against WinUI brushes (e.g. `TextFillColorSecondaryBrush`). For Page background and Label colour the substitution is incomplete (see 10.4.3 / 10.4.4). |
| 10.10.5 | 🟡 | **Three new converters defined but unused on most pages.** | `BoolToVisibilityConverter` (with `Invert`), `AllFalseBoolToVisibilityConverter`, `HasValueConverter`, `InverseBoolConverter`, `InverseBoolToVisibilityConverter` — only some are referenced. Dead code candidates: `InverseBoolConverter`, `InverseBoolToVisibilityConverter`, `AllFalseBoolToVisibilityConverter`, `HasValueConverter`. |

### 10.11 Tests

| # | Severity | Gap | Detail |
|---|---|---|---|
| 10.11.1 | 🟠 | **No Uno‑specific UI/integration tests.** | Xamarin shipped `DailyReflection.UITests` (Xamarin.UITest project). Nothing analogous for the Uno port. Uno.UI tests would normally use `Uno.UITest` or playwright‑style WebAssembly tests; neither is configured. |
| 10.11.2 | 🟠 | **`AutomationProperties.AutomationId` not applied** anywhere in Uno views (see 10.1.8, 10.2.4, 10.3.9). Even if a UI‑test harness is added, today the views provide no stable selectors for it. |
| 10.11.3 | 🟡 | **Test TFMs lag.** | `DailyReflection.Presentation.Tests` and `DailyReflection.Services.Tests` target net9.0 while the shared libraries target net10.0. The CI command in `CLAUDE.md` runs them out‑of‑band. Bump to net10.0 to match. |

### 10.12 Summary punch list (Uno → Xamarin parity)

The minimum work to bring the Uno port to behavioural parity with the Xamarin original, ordered by impact:

1. **Wire `VersionTracking` + run `MigrateSettingsIfNeeded` and `RefreshDatabaseIfNeeded`.** Implement a small `LocalSettings`‑backed version tracker (~20 LOC). Trigger from `App.OnLaunched` after `Host` is built. (10.6.1, 10.6.2, 10.8.1‑3.)
2. **Build a working desktop notification path** — Windows toast via `AppNotificationBuilder` (`Microsoft.Windows.AppNotifications`), macOS via `UNUserNotificationCenter` from the AppKit bridge, Linux via `libnotify` or skip with a clear toast/inline message. At minimum gate the `NotificationsEnabled` toggle on `CanScheduleNotifications` so the user can't silently enable a no‑op. (10.5.1, 10.3.2.)
3. **Render reflection HTML emphasis** — port the DB strings through a small inline parser into a `RichTextBlock`, or strip and italicise the reading + the copyright line specifically. (10.1.1.)
4. **Re‑apply original Shell theme** — Page background + Label foreground + tab bar background/foreground match the `#1976D2`/`#EFF2F5`/`#121212` palette per platform. (10.4.2, 10.4.3, 10.4.4, 10.10.4.)
5. **Add `AutomationProperties.AutomationId` to every control** referenced in `AutomationConstants`. Free, mechanical, unblocks UI tests. (10.1.8, 10.2.4, 10.3.9, 10.11.2.)
6. **Fix `NotificationsEnabled` UX on Android** to match `TimePickerLabelEnabledConverter` (always tappable on Android). Port the converter or special‑case `NotificationTime_Tapped`. (10.3.5.)
7. **Fix `NotificationTime` time‑setter** so it preserves the date component instead of re‑basing to today. (10.3.4.)
8. **Stop the `WeakReferenceMessenger` recipient leak** on Settings — call `vm.IsActive = false` and `WeakReferenceMessenger.Default.UnregisterAll(this)` on `Unloaded` (and set the same pattern on Sober Time, 10.2.2). (10.3.1, 10.2.6.)
9. **Make `ShareService` thread‑safe** — capture title/body in the `DataRequested` handler closure, not on instance fields; register the handler once. Add fallback for non‑`IsSupported` platforms. (10.7.1, 10.7.2.)
10. **Replace hard‑coded `AppVersion`** with the runtime build version once `VersionTracking` is in place. (10.3.7.)
11. **Drop the three extra Android permissions** (`SCHEDULE_EXACT_ALARM`, `USE_EXACT_ALARM`, `WAKE_LOCK`) unless Uno specifically needs them. (10.5.14, 10.9.6.)
12. **Restore iOS supported orientations** to portrait + portrait‑upside‑down on iPhone (no landscape on phones). (10.9.2.)
13. **Realign iOS notification options** — drop `Badge | Sound` from `RequestAuthorizationAsync` to match the original `Alert`‑only request, *or* add a one‑off explanatory copy in the permission dialog. (10.5.3.)
14. **Decide `CancelNotifications` aggressiveness** — keep the `RemoveAllDeliveredNotifications` + badge reset (it's the user‑friendly choice), or revert to the original's `RemoveAllPendingNotificationRequests`‑only semantics. Document either way. (10.5.5.)
15. **Move static `App.GetService<T>()` calls to constructor‑injected views** registered through `ViewMap<TPage, TViewModel>` so DI is honoured by the navigation framework rather than punched through with a service locator. (10.8.8.)
16. **Add `InitializeComponent` ordering and `Loaded`/`Unloaded` symmetry** on every page (set `IsActive = true` in `Loaded`, `false` in `Unloaded`) to match Xamarin's `OnAppearing`/`OnDisappearing` discipline. (10.1.5, 10.2.2, 10.3.1.)
17. **Delete the unused `Resources/PluralityConverter` from `DailyReflectionPage` and the dead `OnUnloaded` handler from `SobrietyTimePage`.** (10.1.10, 10.2.6.)
18. **Either remove the unused converters in App.xaml** (`InverseBoolConverter`, `InverseBoolToVisibilityConverter`, `AllFalseBoolToVisibilityConverter`, `HasValueConverter`) **or actually use them** (e.g., `AllFalseBoolToVisibilityConverter` could replace the `ShowContent` helper on `DailyReflectionPage`). (10.10.5.)
19. **Bump tests to net10.0** and add at minimum smoke tests for the WeakReferenceMessenger registrations and `SettingsService` round‑trip. (10.11.3.)
20. **Document the bundle id change** (`com.kazo0.dailyreflectionuno`) so it is not accidentally reverted to the Xamarin id. If the intent is *replacement* on the App/Play Store, change to `com.kazo0.dailyreflection`; if the intent is *coexistence* during pilot, keep the suffix and add it to the README. (10.9.1.)

The 🔴 items (10.1.1, 10.1.2, 10.2.1, 10.3.1, 10.3.2, 10.3.3, 10.5.1‑3, 10.6.1‑2, 10.7.1‑2, 10.8.1‑3, 10.10.1) are the load‑bearing parity blockers — fix these first.

---

*Report generated by reading the three implementations directly and cross‑checking with `heads/DailyReflection.Uno/genlog.md`. The Avalonia head was not analysed.*
