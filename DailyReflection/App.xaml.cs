using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using DailyReflection.Data.Databases;
using DailyReflection.Services.DailyReflection;
using DailyReflection.Services.Notification;
using DailyReflection.Services.Settings;
using DailyReflection.Services.Share;
using DailyReflection.Presentation.ViewModels;
using DailyReflection.Views;
using System.Reflection;
using Uno.Resizetizer;

namespace DailyReflection;

/// <summary>
/// Daily Reflection app for Uno Platform.
/// Migrated from .NET MAUI with the following key changes:
/// - Shell navigation replaced with TabBar from Uno.Toolkit
/// - Preferences replaced with ApplicationData.LocalSettings (Uno Docs: features/settings.md)
/// - Share replaced with DataTransferManager (Uno Docs: features/windows-applicationmodel-datatransfer.md)
/// - DI configured via Uno.Extensions.Hosting (Uno Docs: HostingSetup.howto.md)
/// </summary>
public partial class App : Application
{
    public static IServiceProvider? ServiceProvider { get; private set; }
    public IHost? Host { get; private set; }

    /// <summary>
    /// Gets a service from the DI container.
    /// Helper method to simplify service resolution from pages.
    /// </summary>
    public static T GetService<T>() where T : class
    {
        if (ServiceProvider == null)
        {
            throw new InvalidOperationException("ServiceProvider is not initialized.");
        }
        
        return ServiceProvider.GetService<T>() 
            ?? throw new InvalidOperationException($"Service of type {typeof(T).Name} is not registered.");
    }

    /// <summary>
    /// Initializes the singleton application object.
    /// </summary>
    public App()
    {
        this.InitializeComponent();
    }

    protected Window? MainWindow { get; private set; }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        // Build configuration from embedded appsettings.json
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("DailyReflection.appsettings.json");
        
        IConfiguration config;
        if (stream != null)
        {
            config = new ConfigurationBuilder()
                .AddJsonStream(stream)
                .Build();
        }
        else
        {
            config = new ConfigurationBuilder().Build();
        }

        // Build DI container
        var services = new ServiceCollection();
        
        // Add configuration
        services.AddSingleton<IConfiguration>(config);
        
        // Add services from shared projects
        services.AddSingleton<IDailyReflectionDatabase, DailyReflection.Data.Databases.DailyReflectionDatabase>();
        services.AddTransient<IDailyReflectionService, DailyReflectionService>();
        
        // Add platform-specific services (Uno implementations)
        services.AddSingleton<ISettingsService, PlatformServices.SettingsService>();
        services.AddSingleton<IShareService, PlatformServices.ShareService>();
        services.AddTransient<INotificationService, PlatformServices.NotificationService>();
        
        // Add ViewModels from shared Presentation project
        services.AddSingleton<DailyReflectionViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<SobrietyTimeViewModel>();
        
        // Add Pages/Views
        services.AddTransient<MainPage>();
        services.AddTransient<DailyReflectionPage>();
        services.AddTransient<SettingsPage>();
        services.AddTransient<SobrietyTimePage>();
        
        ServiceProvider = services.BuildServiceProvider();

        MainWindow = new Window();
#if DEBUG
        MainWindow.UseStudio();
#endif

        // Navigate to the MainPage which hosts the TabBar navigation
        if (MainWindow.Content is not Frame rootFrame)
        {
            rootFrame = new Frame();
            MainWindow.Content = rootFrame;
            rootFrame.NavigationFailed += OnNavigationFailed;
        }

        if (rootFrame.Content == null)
        {
            rootFrame.Navigate(typeof(MainPage), args.Arguments);
        }

        MainWindow.SetWindowIcon();
        MainWindow.Activate();
    }

    void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
    {
        throw new InvalidOperationException($"Failed to load {e.SourcePageType.FullName}: {e.Exception}");
    }

    public static void InitializeLogging()
    {
#if DEBUG
        var factory = LoggerFactory.Create(builder =>
        {
#if __WASM__
            builder.AddProvider(new global::Uno.Extensions.Logging.WebAssembly.WebAssemblyConsoleLoggerProvider());
#elif __IOS__
            builder.AddProvider(new global::Uno.Extensions.Logging.OSLogLoggerProvider());
            builder.AddConsole();
#else
            builder.AddConsole();
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
#endif
    }
}
