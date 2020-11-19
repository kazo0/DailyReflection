using DailyReflection.ViewModels;
using DailyReflection.Views;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Reflection;
using Xamarin.Forms;

namespace DailyReflection.Extensions
{
	public static class ServiceCollectionExtensions
	{
		public static void AddViewModels(this IServiceCollection services)
		{
			services.AddAllSubclassesOf<ViewModelBase>(typeof(ViewModelBase).Assembly, ServiceLifetime.Singleton);
		}

		public static void AddPages(this IServiceCollection services)
		{
			services.AddAllSubclassesOf<Page>(typeof(AppShell).Assembly, ServiceLifetime.Singleton);
		}

		public static void AddAllSubclassesOf<T>(
			this IServiceCollection services,
			Assembly assembly,
			ServiceLifetime lifetime = ServiceLifetime.Transient)
		{
			var types = assembly
				.DefinedTypes
				.Where(t => t.IsSubclassOf(typeof(T)) &&
							!t.IsAbstract);

			foreach (var type in types)
			{
				services.Add(new ServiceDescriptor(type.AsType(), type.AsType(), lifetime));
			}
		}
	}
}
