using System;

namespace DailyReflection.Services.Review;

/// <summary>
/// Decides when the app may ask for a store rating (Spec 012 §A). Pure: the caller supplies
/// today's date and the stored state, so every rule is unit-testable.
/// </summary>
/// <remarks>
/// The OS has the final say — Google Play and StoreKit both throttle the dialog and may show
/// nothing — so these rules only keep the app from asking too early or too often.
/// </remarks>
public static class ReviewPromptPolicy
{
	/// <summary>Distinct days of use before the first request.</summary>
	public const int MinimumUseDays = 5;

	/// <summary>Days since the first recorded use before the first request.</summary>
	public const int MinimumDaysSinceFirstUse = 14;

	/// <summary>Days between two requests.</summary>
	public const int CooldownDays = 120;

	/// <summary>
	/// Counts <paramref name="today"/> as a day of use. Repeat launches on the same day count
	/// once. A date earlier than the last recorded use (the clock or time zone moved back) is
	/// ignored rather than counted again.
	/// </summary>
	public static ReviewPromptState RecordUse(ReviewPromptState state, DateOnly today)
	{
		if (state.LastUseDate is { } last && today <= last)
		{
			return state;
		}

		return state with
		{
			FirstUseDate = state.FirstUseDate ?? today,
			LastUseDate = today,
			UseDayCount = state.UseDayCount + 1,
		};
	}

	/// <summary>
	/// Whether a review may be requested today: the app has been used on at least
	/// <see cref="MinimumUseDays"/> distinct days, the first of them at least
	/// <see cref="MinimumDaysSinceFirstUse"/> days ago, this is not the first launch of a new
	/// version, and the last request was at least <see cref="CooldownDays"/> days ago.
	/// </summary>
	public static bool IsEligible(ReviewPromptState state, DateOnly today, bool isFirstLaunchForCurrentVersion)
	{
		if (isFirstLaunchForCurrentVersion || state.FirstUseDate is not { } firstUse)
		{
			return false;
		}

		if (state.UseDayCount < MinimumUseDays
			|| DaysBetween(firstUse, today) < MinimumDaysSinceFirstUse)
		{
			return false;
		}

		return state.LastPromptDate is not { } lastPrompt
			|| DaysBetween(lastPrompt, today) >= CooldownDays;
	}

	private static int DaysBetween(DateOnly from, DateOnly to) => to.DayNumber - from.DayNumber;
}
