using DailyReflection.Data.Databases;
using Microsoft.Extensions.DependencyInjection;

namespace DailyReflection.Data.DependencyInjection;

public static class Dependencies
{
	public static void AddDataDependencies(this IServiceCollection services)
	{
		services.AddSingleton<IDailyReflectionDatabase, DailyReflectionDatabase>();
	}
}
