using DailyReflection.DependencyInjection;
using DailyReflection.Presentation;
using DailyReflection.Presentation.DependencyInjection;
using DailyReflection.Presentation.Models;
using DailyReflection.Services.Startup;
using DailyReflection.Views;
using Microsoft.Extensions.Configuration;
using System.Diagnostics.CodeAnalysis;

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

	public App()
	{
		this.InitializeComponent();
	}

	/// <summary>
	/// Primary application window. Exposed publicly so platform services
	/// (e.g. <c>ShareService</c>'s unsupported-platform fallback) can resolve
	/// a <c>XamlRoot</c> for hosting <c>ContentDialog</c>s without hard-coding
	/// access to internal members.
	/// </summary>
	public Window? MainWindow { get; private set; }

	[RequiresUnreferencedCode()]
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
				.UseNavigation(ReactiveViewModelMappings.ViewModelMappings, RegisterRoutes));

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
	// so a fresh DI resolution per route is correct. The MVUX models are
	// Singleton (see AddPresentationDependencies); the navigator resolves the
	// model and wraps it in the generated Bindable*Model view-model.
	private static void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
	{
		views.Register(
			new ViewMap<MainPage>(),
			new ViewMap<DailyReflectionPage, DailyReflectionModel>(),
			new ViewMap<SobrietyTimePage, SobrietyTimeModel>(),
			new ViewMap<SettingsPage, SettingsModel>());

		routes.Register(
			new RouteMap("Main", View: views.FindByView<MainPage>(), IsDefault: true,
				Nested:
				[
					new RouteMap("Reflection", View: views.FindByViewModel<DailyReflectionModel>(), IsDefault: true),
					new RouteMap("SoberTime", View: views.FindByViewModel<SobrietyTimeModel>()),
					new RouteMap("Settings", View: views.FindByViewModel<SettingsModel>()),
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
