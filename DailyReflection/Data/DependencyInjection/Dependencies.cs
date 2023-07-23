using DailyReflection.Data.Databases;

namespace DailyReflection.Data.DependencyInjection
{
	public static class Dependencies
	{
		public static MauiAppBuilder AddDataDependencies(this MauiAppBuilder builder)
		{
			builder.Services.AddSingleton<IDailyReflectionDatabase, DailyReflectionDatabase>();

			return builder;
		}
	}
}
