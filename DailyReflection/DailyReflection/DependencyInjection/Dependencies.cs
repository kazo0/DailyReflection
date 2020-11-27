using DailyReflection.Core.Extensions;
using DailyReflection.Views;
using Microsoft.Extensions.DependencyInjection;
using Xamarin.Forms;

namespace DailyReflection.DependencyInjection
{
	public static class Dependencies
	{
		public static void AddPages(this IServiceCollection services)
		{
			services.AddAllSubclassesOf<Page>(typeof(AppShell).Assembly, ServiceLifetime.Singleton);
		}
	}
}
