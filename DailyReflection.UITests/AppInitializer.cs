using System;
using Xamarin.UITest;
using Xamarin.UITest.Queries;

namespace DailyReflection.UITests
{
    public class AppInitializer
    {
        public static IApp StartApp(Platform platform)
        {
            if (platform == Platform.Android)
            {
                return ConfigureApp
                    .Android
                    .InstalledApp("com.kazo0.dailyreflection")
                    .StartApp();
            }

            return ConfigureApp.iOS.StartApp();
        }
    }
}