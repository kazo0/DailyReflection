
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using DailyReflection.Data;
using DailyReflection.Extensions;
using DailyReflection.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DailyReflection
{
	public static class Startup
	{
		public static IServiceProvider ServiceProvider { get; set; }

		public static void Init(Action<HostBuilderContext, IServiceCollection> platformConfigure)
		{
			var host = Host.CreateDefaultBuilder()
				.ConfigureAppConfiguration(config =>
				{
					config.SetFileProvider(new EmbeddedFileProvider(typeof(App).Assembly));
					config.AddJsonFile("appsettings.json");
				})
				.ConfigureServices((ctx, sc) => 
				{
					ConfigureServices(ctx, sc);
					platformConfigure?.Invoke(ctx, sc);

				})
				.ConfigureLogging(builder =>
				{
					builder.AddConsole();
				})
				.Build();

			ServiceProvider = host.Services;
		}

		private static void ConfigureServices(HostBuilderContext ctx, IServiceCollection services)
		{
			services.AddSingleton<IDailyReflectionDatabase, DailyReflectionDatabase>();
			services.AddTransient<IDailyReflectionService, DailyReflectionService>();

			services.AddViewModels();
			services.AddPages();
		}
	}
}