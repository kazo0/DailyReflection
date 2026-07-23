using DailyReflection.DependencyInjection;
using DailyReflection.Presentation.DependencyInjection;
using DailyReflection.Presentation.ViewModels;
using DailyReflection.Services.Startup;
using DailyReflection.Views;
using Microsoft.Extensions.Configuration;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Uno.Extensions;
using Uno.Extensions.Configuration;
using Uno.Extensions.Hosting;
using Uno.Extensions.Navigation;
using Uno.Resizetizer;
using Windows.UI;

namespace DailyReflection;

/// <summary>
/// Daily Reflection app for Uno Platform.
/// Wires up the canonical Uno.Extensions stack:
///   - UseConfiguration with EmbeddedSource&lt;App&gt;() loads appsettings.json
///   - UseToolkitNavigation + UseNavigation drive region-based TabBar routing
///   - ConfigureServices delegates to the shared AddPresentationDependencies chain
/// </summary>
public partial class App : Application
{
    public IHost? Host { get; private set; }

    /// <summary>
    /// Resolve a service from the host's DI container.
    /// </summary>
    public static T GetService<T>() where T : class
    {
        var host = ((App)Current).Host
            ?? throw new InvalidOperationException("Host is not initialized.");

        return host.Services.GetService<T>()
            ?? throw new InvalidOperationException($"Service of type {typeof(T).Name} is not registered.");
    }

    public App()
    {
        this.InitializeComponent();
        ApplyCommandBarChrome();
    }

    /// <summary>
    /// Spec 004 §D — restore the Xamarin Shell chrome on the (closest) WinUI/Uno
    /// surface, the <c>CommandBar</c>. On Android the original used the primary
    /// blue <c>#1976D2</c> as background with white foreground; on iOS it used
    /// the page background colour with the iOS system blue <c>#007BFF</c> as
    /// foreground. WinUI doesn't support OnPlatform in XAML resources, so the
    /// per-platform branching happens here at startup.
    /// </summary>
    private void ApplyCommandBarChrome()
    {
        if (Current.Resources is null)
        {
            return;
        }

        SolidColorBrush backgroundBrush;
        SolidColorBrush foregroundBrush;

        if (OperatingSystem.IsAndroid())
        {
            backgroundBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0x19, 0x76, 0xD2));
            foregroundBrush = new SolidColorBrush(Colors.White);
        }
        else if (OperatingSystem.IsIOS())
        {
            backgroundBrush = (Current.Resources["DRPageBackgroundBrush"] as SolidColorBrush)
                ?? new SolidColorBrush(Color.FromArgb(0xFF, 0xEF, 0xF2, 0xF5));
            foregroundBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0x00, 0x7B, 0xFF));
        }
        else
        {
            // Desktop: align with the page background and primary text colour
            // so the bar reads consistently with the rest of the page.
            backgroundBrush = (Current.Resources["DRPageBackgroundBrush"] as SolidColorBrush)
                ?? new SolidColorBrush(Colors.White);
            foregroundBrush = (Current.Resources["DRTextPrimaryBrush"] as SolidColorBrush)
                ?? new SolidColorBrush(Colors.Black);
        }

        Current.Resources["DRCommandBarBackgroundBrush"] = backgroundBrush;
        Current.Resources["DRCommandBarForegroundBrush"] = foregroundBrush;
    }

    /// <summary>
    /// Primary application window. Exposed publicly so platform services
    /// (e.g. <c>ShareService</c>'s unsupported-platform fallback) can resolve
    /// a <c>XamlRoot</c> for hosting <c>ContentDialog</c>s without hard-coding
    /// access to internal members.
    /// </summary>
    public Window? MainWindow { get; private set; }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        var builder = this.CreateBuilder(args)
            .UseToolkitNavigation()
            .Configure(host => host
                .UseConfiguration(configure: configBuilder =>
                    configBuilder.EmbeddedSource<App>())
                .ConfigureServices((context, services) =>
                {
                    services.AddPlatformServices();
                    services.AddPresentationDependencies();

                    services.AddTransient<MainPage>();
                    services.AddTransient<DailyReflectionPage>();
                    services.AddTransient<SettingsPage>();
                    services.AddTransient<SobrietyTimePage>();
                })
                .UseNavigation(RegisterRoutes));

        MainWindow = builder.Window;

        MainWindow.SetWindowIcon();

        Host = await builder.NavigateAsync<MainPage>();

        // Spec 011 §G — fail loudly when appsettings.json is missing or the
        // database key isn't populated, instead of silently defaulting and
        // hitting a confusing error in the database layer later.
        var configuration = Host.Services.GetRequiredService<IConfiguration>();
        if (string.IsNullOrEmpty(configuration[Core.Constants.ConfigurationConstants.DatabaseFileName]))
        {
            throw new InvalidOperationException(
                $"Configuration is missing required key '{Core.Constants.ConfigurationConstants.DatabaseFileName}'. " +
                "Ensure appsettings.json is embedded in the assembly.");
        }

        // Run version-gated startup migrations (port of Xamarin App.OnStart).
        // Done after navigation so first paint isn't blocked; migrations themselves
        // are async and operate on stores guarded internally.
        try
        {
            var runner = Host.Services.GetRequiredService<StartupMigrationRunner>();
            await runner.RunAsync();
        }
        catch (Exception ex)
        {
            this.Log().LogError(ex, "Startup migrations failed.");
        }
    }

    // Pages are registered Transient (see ConfigureServices). Uno.Extensions
    // Navigation expects fresh page instances per region activation; the
    // Visibility navigator on MainPage caches the materialised view itself,
    // so a fresh DI resolution per route is correct. The Xamarin original
    // used Singleton-scoped pages because Shell held one of each — that
    // model does not apply here.
    private static void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
    {
        views.Register(
            new ViewMap<MainPage>(),
            new ViewMap<DailyReflectionPage, DailyReflectionViewModel>(),
            new ViewMap<SobrietyTimePage, SobrietyTimeViewModel>(),
            new ViewMap<SettingsPage, SettingsViewModel>());

        routes.Register(
            new RouteMap("Main", View: views.FindByView<MainPage>(), IsDefault: true,
                Nested:
                [
                    new RouteMap("Reflection", View: views.FindByViewModel<DailyReflectionViewModel>(), IsDefault: true),
                    new RouteMap("SoberTime", View: views.FindByViewModel<SobrietyTimeViewModel>()),
                    new RouteMap("Settings", View: views.FindByViewModel<SettingsViewModel>()),
                ]));
    }

    public static void InitializeLogging()
    {
        // Spec 011 §F — release builds also create the LoggerFactory so logs
        // surface in production. Provider selection still varies by platform
        // and is scoped to DEBUG to keep release output reasonable.
        var factory = LoggerFactory.Create(builder =>
        {
#if DEBUG
#if __WASM__
            builder.AddProvider(new global::Uno.Extensions.Logging.WebAssembly.WebAssemblyConsoleLoggerProvider());
#elif __IOS__
            builder.AddProvider(new global::Uno.Extensions.Logging.OSLogLoggerProvider());
            builder.AddConsole();
#else
            builder.AddConsole();
#endif
#endif

            builder.SetMinimumLevel(LogLevel.Information);
            builder.AddFilter("Uno", LogLevel.Warning);
            builder.AddFilter("Windows", LogLevel.Warning);
            builder.AddFilter("Microsoft", LogLevel.Warning);
        });

        global::Uno.Extensions.LogExtensionPoint.AmbientLoggerFactory = factory;

#if HAS_UNO
        global::Uno.UI.Adapter.Microsoft.Extensions.Logging.LoggingAdapter.Initialize();
#endif
    }
}
