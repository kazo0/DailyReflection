using CommunityToolkit.Mvvm.Messaging;
using DailyReflection.Presentation.Models;
using DailyReflection.Services.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;

namespace DailyReflection.Presentation.DependencyInjection;

public static class Dependencies
{
	public static void AddPresentationDependencies(this IServiceCollection services)
	{
		services.AddSingleton<IMessenger, WeakReferenceMessenger>();

		// MVUX models are registered Singleton — same lifetime choice as the
		// ViewModels they replace, so tab state survives region switches.
		services.AddSingleton<SettingsModel>();
		services.AddSingleton<SobrietyTimeModel>();
		services.AddSingleton<DailyReflectionModel>();

		// ... and so are the generated view-models that wrap them (bindable
		// generation tool v3 names them {Name}ViewModel).
		// The navigator resolves view models from DI first and only falls back
		// to constructing them — via the generated services-ctor, which would
		// new up a *second* instance of the model (breaking the SettingsModel
		// states shared with SobrietyTimeModel). Registering the bindables
		// here, bound to the singleton models, keeps a single source of truth.
		services.AddSingleton(sp => CreateBindable<SettingsViewModel>(sp.GetRequiredService<SettingsModel>()));
		services.AddSingleton(sp => CreateBindable<SobrietyTimeViewModel>(sp.GetRequiredService<SobrietyTimeModel>()));
		services.AddSingleton(sp => CreateBindable<DailyReflectionViewModel>(sp.GetRequiredService<DailyReflectionModel>()));

		services.AddServiceDependencies();
	}

	// The MVUX generator emits the model-wrapping ctor as protected.
	private static TBindable CreateBindable<TBindable>(object model) where TBindable : class
		=> (TBindable)Activator.CreateInstance(
			typeof(TBindable),
			BindingFlags.Instance | BindingFlags.NonPublic,
			binder: null,
			args: new[] { model },
			culture: null)!;
}
