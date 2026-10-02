using System.Threading;
using System.Threading.Tasks;

namespace DailyReflection.Services.Review;

/// <summary>
/// Asks engaged users to rate the app (Spec 012). Tracks the days the app is used and
/// requests the platform rating dialog when <see cref="ReviewPromptPolicy"/> allows it.
/// </summary>
public interface IReviewPromptService
{
	/// <summary>Counts today as a day of use. Call on every launch and resume.</summary>
	void RecordUse();

	/// <summary>
	/// Requests the platform rating dialog if the app is eligible today. Must run after
	/// <c>IVersionTrackingService.Track</c> (the startup migrations call it).
	/// </summary>
	/// <returns><c>true</c> when the dialog was requested (the OS may still not show it).</returns>
	Task<bool> TryRequestReviewAsync(CancellationToken cancellationToken);
}
