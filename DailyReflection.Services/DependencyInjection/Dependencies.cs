﻿using DailyReflection.Data.DependencyInjection;
using DailyReflection.Services.DailyReflection;
using DailyReflection.Services.Settings;
using DailyReflection.Services.Share;
using Microsoft.Extensions.DependencyInjection;

namespace DailyReflection.Services.DependencyInjection
{
	public static class Dependencies
	{
		public static void AddServiceDependencies(this IServiceCollection services)
		{
			services.AddDataDependencies();
			services.AddTransient<IDailyReflectionService, DailyReflectionService>();
			services.AddTransient<ISettingsService, SettingsService>();
			services.AddTransient<IShareService, ShareService>();
		}
	}
}
