using DailyReflection.Data.DependencyInjection;
using DailyReflection.Services.DailyReflection;
using Microsoft.Extensions.DependencyInjection;



namespace DailyReflection.Services.DependencyInjection;

public static class Dependencies
{
	public static void AddServiceDependencies(this IServiceCollection services)
	{
		services.AddDataDependencies();
		services.AddTransient<IDailyReflectionService, DailyReflectionService>();
	}
}
