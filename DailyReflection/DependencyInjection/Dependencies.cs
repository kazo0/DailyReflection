using DailyReflection.Services.Notification;
using DailyReflection.Services.Settings;
using DailyReflection.Services.Share;
using DailyReflection.Services.Startup;
using DailyReflection.Services.VersionTracking;
using Microsoft.Extensions.DependencyInjection;

namespace DailyReflection.DependencyInjection;

public static class Dependencies
{
    public static void AddPlatformServices(this IServiceCollection services)
    {
        services.AddSingleton<ISettingsService, PlatformServices.SettingsService>();
        services.AddSingleton<IShareService, PlatformServices.ShareService>();
        services.AddTransient<INotificationService, PlatformServices.NotificationService>();
        services.AddSingleton<IVersionTrackingService, PlatformServices.VersionTrackingService>();
        services.AddTransient<StartupMigrationRunner>();
    }
}
