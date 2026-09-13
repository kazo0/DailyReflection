using CommunityToolkit.Mvvm.Messaging;
using DailyReflection.Services.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace DailyReflection.Presentation.DependencyInjection;

public static class Dependencies
{
	public static void AddPresentationDependencies(this IServiceCollection services)
	{
		// The MVUX models and their generated {Name}ViewModel view-models are not
		// registered here: each ViewMap in App.RegisterRoutes registers them
		// Transient, after this runs, so those registrations would win anyway.
		// Tabs stay in sync through this singleton messenger instead of shared
		// model instances.
		services.AddSingleton<IMessenger, WeakReferenceMessenger>();

		services.AddServiceDependencies();
	}
}
