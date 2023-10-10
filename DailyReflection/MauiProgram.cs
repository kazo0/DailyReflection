using CommunityToolkit.Maui;
using DailyReflection.Data.DependencyInjection;
using DailyReflection.DependencyInjection;
using DailyReflection.Presentation.DependencyInjection;
using DailyReflection.Services.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Handlers;

namespace DailyReflection
{
	public static class MauiProgram
	{
		public static MauiApp CreateMauiApp()
		{
			var builder = MauiApp.CreateBuilder();
			builder
				.UseMauiCommunityToolkit()
				.UseMauiApp<App>()
				.AddPages()
				.AddPresentationDependencies()
				.AddDataDependencies()
				.AddServiceDependencies()
				.ConfigureFonts(fonts =>
				{
					fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
					fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
					fonts.AddFont("FontAwesomeBrands.otf", "FaBrandsFont");
					fonts.AddFont("FontAwesomeRegular.otf", "FaRegularFont");
					fonts.AddFont("FontAwesomeSolid.otf", "FaSolidFont");
				});

#if IOS14_0_OR_GREATER
			DatePickerHandler.Mapper.AppendToMapping("NotWheels", (handler, datePicker) =>
			{
				if (handler.PlatformView.InputView as UIKit.UIDatePicker is { } native)
				{
					if (OperatingSystem.IsIOSVersionAtLeast(14))
					{
						native.PreferredDatePickerStyle = UIKit.UIDatePickerStyle.Inline;
					}
				}
			});
#endif
#if DEBUG
			builder.Logging.AddDebug();
#endif

			return builder.Build();
		}
	}
}