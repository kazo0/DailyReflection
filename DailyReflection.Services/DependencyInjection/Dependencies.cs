using DailyReflection.Data.DependencyInjection;
using DailyReflection.Services.DailyReflection;
using DailyReflection.Services.Review;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;



namespace DailyReflection.Services.DependencyInjection;

public static class Dependencies
{
	public static void AddServiceDependencies(this IServiceCollection services)
	{
		services.AddDataDependencies();
		services.AddTransient<IDailyReflectionService, DailyReflectionService>();
		// Spec 012 — the platform IStoreReviewService and IReviewPromptStateStore come from
		// the head's AddPlatformServices.
		services.TryAddSingleton(TimeProvider.System);
		services.AddTransient<IReviewPromptService, ReviewPromptService>();
	}
}
