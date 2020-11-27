using DailyReflection.Data.Databases;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace DailyReflection.Data.DependencyInjection
{
	public static class Dependencies
	{
		public static void AddDataDependencies(this IServiceCollection services)
		{
			services.AddSingleton<IDailyReflectionDatabase, DailyReflectionDatabase>();
		}
	}
}
