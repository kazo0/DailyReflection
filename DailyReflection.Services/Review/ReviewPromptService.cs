using DailyReflection.Services.VersionTracking;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DailyReflection.Services.Review;

/// <summary>
/// Applies <see cref="ReviewPromptPolicy"/> to the stored state and the version tracker, and
/// hands eligible requests to the platform's <see cref="IStoreReviewService"/> (Spec 012 §A).
/// Does nothing on platforms without a rating dialog.
/// </summary>
public class ReviewPromptService : IReviewPromptService
{
	private readonly IReviewPromptStateStore _store;
	private readonly IStoreReviewService _storeReview;
	private readonly IVersionTrackingService _versionTracking;
	private readonly TimeProvider _timeProvider;

	public ReviewPromptService(
		IReviewPromptStateStore store,
		IStoreReviewService storeReview,
		IVersionTrackingService versionTracking,
		TimeProvider timeProvider)
	{
		_store = store;
		_storeReview = storeReview;
		_versionTracking = versionTracking;
		_timeProvider = timeProvider;
	}

	public void RecordUse()
	{
		if (!_storeReview.IsSupported)
		{
			return;
		}

		var state = _store.Load();
		var updated = ReviewPromptPolicy.RecordUse(state, Today);
		if (updated != state)
		{
			_store.Save(updated);
		}
	}

	public async Task<bool> TryRequestReviewAsync(CancellationToken cancellationToken)
	{
		if (!_storeReview.IsSupported)
		{
			return false;
		}

		var today = Today;
		if (!IsEligible(_store.Load(), today) || !await _storeReview.CanRequestNowAsync(cancellationToken))
		{
			return false;
		}

		// Checked again after the await: a launch and a resume can overlap, and only the
		// first may ask.
		var state = _store.Load();
		if (!IsEligible(state, today))
		{
			return false;
		}

		// Recorded before asking, so a request that throws or crashes the process still
		// starts the cooldown instead of asking again on the next launch.
		_store.Save(state with { LastPromptDate = today });
		await _storeReview.RequestReviewAsync(cancellationToken);
		return true;
	}

	private bool IsEligible(ReviewPromptState state, DateOnly today)
		=> ReviewPromptPolicy.IsEligible(state, today, _versionTracking.IsFirstLaunchForCurrentVersion);

	private DateOnly Today => DateOnly.FromDateTime(_timeProvider.GetLocalNow().DateTime);
}
