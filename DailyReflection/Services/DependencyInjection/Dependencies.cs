using DailyReflection.Services.DailyReflection;
using DailyReflection.Services.Notification;
using DailyReflection.Services.Settings;
using DailyReflection.Services.Share;

namespace DailyReflection.Services.DependencyInjection
{
	public static class Dependencies
	{
		public static MauiAppBuilder AddServiceDependencies(this MauiAppBuilder builder)
		{
			builder.Services
				.AddTransient<IDailyReflectionService, DailyReflectionService>()
				.AddTransient<INotificationService, NotificationService>()
				.AddTransient<ISettingsService, SettingsService>()
				.AddTransient<IShareService, ShareService>();

			return builder;
		}
	}
}
