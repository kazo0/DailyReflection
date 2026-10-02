using DailyReflection.Services.Review;
using DailyReflection.Services.VersionTracking;
using Moq;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DailyReflection.Services.Tests.Review;

[TestFixture]
public class ReviewPromptServiceTests : ServiceTestBase<ReviewPromptService>
{
	private static readonly DateOnly Today = new(2026, 6, 15);

	private Mock<IStoreReviewService> _storeReview = null!;
	private Mock<IVersionTrackingService> _versionTracking = null!;
	private InMemoryStateStore _store = null!;
	private FixedTimeProvider _timeProvider = null!;

	/// <summary>Eligible today: used on 5 days, the first of them 30 days ago.</summary>
	private static readonly ReviewPromptState EligibleState = new(Today.AddDays(-30), Today.AddDays(-1), 5, null);

	protected override ReviewPromptService GetService()
	{
		_storeReview = new Mock<IStoreReviewService>();
		_storeReview.SetupGet(s => s.IsSupported).Returns(true);
		_storeReview.Setup(s => s.CanRequestNowAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
		_versionTracking = new Mock<IVersionTrackingService>();
		_store = new InMemoryStateStore();
		_timeProvider = new FixedTimeProvider(Today);
		return new ReviewPromptService(_store, _storeReview.Object, _versionTracking.Object, _timeProvider);
	}

	[Test]
	public void RecordUse_counts_today_once()
	{
		ServiceUnderTest.RecordUse();
		ServiceUnderTest.RecordUse();

		Assert.That(_store.State, Is.EqualTo(new ReviewPromptState(Today, Today, 1, null)));
	}

	[Test]
	public async Task An_eligible_launch_requests_a_review_and_starts_the_cooldown()
	{
		_store.State = EligibleState;
		_storeReview
			.Setup(s => s.RequestReviewAsync(It.IsAny<CancellationToken>()))
			.Callback(() => Assert.That(_store.State.LastPromptDate, Is.EqualTo(Today), "The cooldown is saved before the OS is asked."))
			.Returns(Task.CompletedTask);

		var requested = await ServiceUnderTest.TryRequestReviewAsync(CancellationToken.None);

		Assert.That(requested, Is.True);
		_storeReview.Verify(s => s.RequestReviewAsync(It.IsAny<CancellationToken>()), Times.Once);
	}

	[Test]
	public async Task A_second_request_the_same_day_is_refused_by_the_cooldown()
	{
		_store.State = EligibleState;

		await ServiceUnderTest.TryRequestReviewAsync(CancellationToken.None);
		var again = await ServiceUnderTest.TryRequestReviewAsync(CancellationToken.None);

		Assert.That(again, Is.False);
		_storeReview.Verify(s => s.RequestReviewAsync(It.IsAny<CancellationToken>()), Times.Once);
	}

	[Test]
	public async Task The_first_launch_of_a_new_version_does_not_request_a_review()
	{
		_store.State = EligibleState;
		_versionTracking.SetupGet(v => v.IsFirstLaunchForCurrentVersion).Returns(true);

		var requested = await ServiceUnderTest.TryRequestReviewAsync(CancellationToken.None);

		Assert.Multiple(() =>
		{
			Assert.That(requested, Is.False);
			Assert.That(_store.State, Is.EqualTo(EligibleState));
		});
		_storeReview.Verify(s => s.RequestReviewAsync(It.IsAny<CancellationToken>()), Times.Never);
	}

	[Test]
	public async Task Leaving_the_app_before_the_request_does_not_start_the_cooldown()
	{
		_store.State = EligibleState;
		_storeReview.Setup(s => s.CanRequestNowAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);

		var requested = await ServiceUnderTest.TryRequestReviewAsync(CancellationToken.None);

		Assert.Multiple(() =>
		{
			Assert.That(requested, Is.False);
			Assert.That(_store.State, Is.EqualTo(EligibleState));
		});
		_storeReview.Verify(s => s.RequestReviewAsync(It.IsAny<CancellationToken>()), Times.Never);
	}

	[Test]
	public async Task Overlapping_requests_ask_only_once()
	{
		_store.State = EligibleState;
		var ready = new TaskCompletionSource<bool>();
		_storeReview.Setup(s => s.CanRequestNowAsync(It.IsAny<CancellationToken>())).Returns(ready.Task);

		var first = ServiceUnderTest.TryRequestReviewAsync(CancellationToken.None);
		var second = ServiceUnderTest.TryRequestReviewAsync(CancellationToken.None);
		ready.SetResult(true);

		Assert.That(await Task.WhenAll(first, second), Is.EquivalentTo(new[] { true, false }));
		_storeReview.Verify(s => s.RequestReviewAsync(It.IsAny<CancellationToken>()), Times.Once);
	}

	[Test]
	public async Task Unsupported_platforms_neither_track_nor_request()
	{
		_storeReview.SetupGet(s => s.IsSupported).Returns(false);
		_store.State = EligibleState;

		ServiceUnderTest.RecordUse();
		var requested = await ServiceUnderTest.TryRequestReviewAsync(CancellationToken.None);

		Assert.Multiple(() =>
		{
			Assert.That(requested, Is.False);
			Assert.That(_store.SaveCount, Is.Zero);
		});
		_storeReview.Verify(s => s.RequestReviewAsync(It.IsAny<CancellationToken>()), Times.Never);
	}

	[Test]
	public void A_failing_request_still_starts_the_cooldown()
	{
		_store.State = EligibleState;
		_storeReview
			.Setup(s => s.RequestReviewAsync(It.IsAny<CancellationToken>()))
			.ThrowsAsync(new InvalidOperationException("Play Store unavailable"));

		Assert.ThrowsAsync<InvalidOperationException>(() => ServiceUnderTest.TryRequestReviewAsync(CancellationToken.None));
		Assert.That(_store.State.LastPromptDate, Is.EqualTo(Today));
	}

	[Test]
	public void Days_follow_the_local_calendar()
	{
		// 23:30 UTC on the 14th is already the 15th an hour east of UTC.
		_timeProvider.UtcNow = new DateTimeOffset(2026, 6, 14, 23, 30, 0, TimeSpan.Zero);
		_timeProvider.Zone = TimeZoneInfo.CreateCustomTimeZone("UTC+1", TimeSpan.FromHours(1), "UTC+1", "UTC+1");

		ServiceUnderTest.RecordUse();

		Assert.That(_store.State.LastUseDate, Is.EqualTo(Today));
	}

	private sealed class InMemoryStateStore : IReviewPromptStateStore
	{
		public ReviewPromptState State { get; set; } = ReviewPromptState.Empty;

		public int SaveCount { get; private set; }

		public ReviewPromptState Load() => State;

		public void Save(ReviewPromptState state)
		{
			State = state;
			SaveCount++;
		}
	}

	private sealed class FixedTimeProvider(DateOnly today) : TimeProvider
	{
		public DateTimeOffset UtcNow { get; set; } = new(today.ToDateTime(new TimeOnly(12, 0)), TimeSpan.Zero);

		public TimeZoneInfo Zone { get; set; } = TimeZoneInfo.Utc;

		public override DateTimeOffset GetUtcNow() => UtcNow;

		public override TimeZoneInfo LocalTimeZone => Zone;
	}
}
