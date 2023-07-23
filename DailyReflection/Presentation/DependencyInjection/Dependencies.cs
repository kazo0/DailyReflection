using DailyReflection.Extensions;
using DailyReflection.Presentation.ViewModels;

namespace DailyReflection.Presentation.DependencyInjection
{
	public static class Dependencies
	{
		public static MauiAppBuilder AddPresentationDependencies(this MauiAppBuilder builder)
		{
			builder.Services.AddAllSubclassesOf<ViewModelBase>(typeof(ViewModelBase).Assembly, ServiceLifetime.Singleton);

			return builder;
		}
	}
}
