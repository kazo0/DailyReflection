
/* Unmerged change from project 'DailyReflection (net8.0-android)'
Before:
using DailyReflection.Views;
After:
using DailyReflection.Extensions;
using DailyReflection.Views;
*/
using DailyReflection.Extensions;

/* Unmerged change from project 'DailyReflection (net8.0-android)'
Before:
using Microsoft.Maui.Controls;
using Microsoft.Maui;
using DailyReflection.Extensions;
After:
using Microsoft.Maui;
using Microsoft.Maui.Controls;
*/
using DailyReflection.Views;

namespace DailyReflection.DependencyInjection
{
	public static class Dependencies
	{
		public static MauiAppBuilder AddPages(this MauiAppBuilder builder)
		{
			builder.Services.AddAllSubclassesOf<Page>(typeof(AppShell).Assembly, ServiceLifetime.Singleton);

			return builder;
		}
	}
}
