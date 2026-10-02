using DailyReflection.Services.Review;
using NUnit.Framework;
using System;

namespace DailyReflection.Services.Tests.Review;

[TestFixture]
public class ReviewPromptPolicyTests
{
	private static readonly DateOnly FirstUse = new(2026, 3, 1);

	/// <summary>Uses on <paramref name="days"/> distinct days, the first on <see cref="FirstUse"/>.</summary>
	private static ReviewPromptState UsedOn(int days, DateOnly? lastPrompt = null)
	{
		var state = ReviewPromptState.Empty;
		for (var i = 0; i < days; i++)
		{
			state = ReviewPromptPolicy.RecordUse(state, FirstUse.AddDays(i));
		}

		return state with { LastPromptDate = lastPrompt };
	}

	private static DateOnly DaysAfterFirstUse(int days) => FirstUse.AddDays(days);

	[Test]
	public void A_new_install_is_not_eligible()
	{
		Assert.That(ReviewPromptPolicy.IsEligible(ReviewPromptState.Empty, FirstUse, isFirstLaunchForCurrentVersion: false), Is.False);
	}

	[Test]
	public void Too_few_days_of_use_is_not_eligible()
	{
		var state = UsedOn(ReviewPromptPolicy.MinimumUseDays - 1);

		Assert.That(ReviewPromptPolicy.IsEligible(state, DaysAfterFirstUse(60), isFirstLaunchForCurrentVersion: false), Is.False);
	}

	[Test]
	public void Too_soon_after_the_first_use_is_not_eligible()
	{
		var state = UsedOn(10);

		Assert.That(ReviewPromptPolicy.IsEligible(state, DaysAfterFirstUse(ReviewPromptPolicy.MinimumDaysSinceFirstUse - 1), isFirstLaunchForCurrentVersion: false), Is.False);
	}

	[Test]
	public void Exactly_at_both_thresholds_is_eligible()
	{
		var state = UsedOn(ReviewPromptPolicy.MinimumUseDays);

		Assert.Multiple(() =>
		{
			Assert.That(state.UseDayCount, Is.EqualTo(5));
			Assert.That(ReviewPromptPolicy.IsEligible(state, DaysAfterFirstUse(14), isFirstLaunchForCurrentVersion: false), Is.True);
		});
	}

	[Test]
	public void The_first_launch_of_a_new_version_is_not_eligible()
	{
		var state = UsedOn(30);

		Assert.That(ReviewPromptPolicy.IsEligible(state, DaysAfterFirstUse(60), isFirstLaunchForCurrentVersion: true), Is.False);
	}

	[Test]
	public void Within_the_cooldown_after_a_request_is_not_eligible()
	{
		var lastPrompt = DaysAfterFirstUse(20);
		var state = UsedOn(30, lastPrompt);

		Assert.Multiple(() =>
		{
			Assert.That(ReviewPromptPolicy.IsEligible(state, lastPrompt, isFirstLaunchForCurrentVersion: false), Is.False);
			Assert.That(ReviewPromptPolicy.IsEligible(state, lastPrompt.AddDays(ReviewPromptPolicy.CooldownDays - 1), isFirstLaunchForCurrentVersion: false), Is.False);
		});
	}

	[Test]
	public void The_cooldown_ends_after_exactly_120_days()
	{
		var lastPrompt = DaysAfterFirstUse(20);
		var state = UsedOn(30, lastPrompt);

		Assert.That(ReviewPromptPolicy.IsEligible(state, lastPrompt.AddDays(120), isFirstLaunchForCurrentVersion: false), Is.True);
	}

	[Test]
	public void Repeated_launches_on_the_same_day_count_once()
	{
		var state = ReviewPromptState.Empty;
		for (var i = 0; i < 10; i++)
		{
			state = ReviewPromptPolicy.RecordUse(state, FirstUse);
		}

		Assert.Multiple(() =>
		{
			Assert.That(state.UseDayCount, Is.EqualTo(1));
			Assert.That(ReviewPromptPolicy.IsEligible(state, DaysAfterFirstUse(30), isFirstLaunchForCurrentVersion: false), Is.False);
		});
	}

	[Test]
	public void Recording_use_tracks_the_first_and_last_day()
	{
		var state = ReviewPromptPolicy.RecordUse(ReviewPromptState.Empty, FirstUse);
		state = ReviewPromptPolicy.RecordUse(state, DaysAfterFirstUse(3));

		Assert.That(state, Is.EqualTo(new ReviewPromptState(FirstUse, DaysAfterFirstUse(3), 2, null)));
	}

	[Test]
	public void A_date_before_the_last_use_is_not_counted()
	{
		var state = UsedOn(3);

		Assert.That(ReviewPromptPolicy.RecordUse(state, FirstUse), Is.SameAs(state));
	}
}
