using System;

namespace DailyReflection.Services.Review;

/// <summary>
/// What the in-app review prompt remembers between launches (Spec 012 §A). All dates are
/// local calendar days.
/// </summary>
/// <param name="FirstUseDate">The first day the app was used since review tracking began.</param>
/// <param name="LastUseDate">The most recent day the app was used.</param>
/// <param name="UseDayCount">How many distinct days the app has been used on.</param>
/// <param name="LastPromptDate">The last day a review was requested, or <c>null</c> if never.</param>
public sealed record ReviewPromptState(
	DateOnly? FirstUseDate,
	DateOnly? LastUseDate,
	int UseDayCount,
	DateOnly? LastPromptDate)
{
	public static ReviewPromptState Empty { get; } = new(null, null, 0, null);
}
