using System.Threading;
using System.Threading.Tasks;

namespace DailyReflection.Services.Review;

/// <summary>
/// The platform's in-app rating dialog: the Google Play In-App Review API on Android,
/// <c>SKStoreReviewController</c> on iOS (Spec 012 §C). Desktop and WebAssembly have none.
/// </summary>
public interface IStoreReviewService
{
	/// <summary>Whether this platform has an in-app rating dialog at all.</summary>
	bool IsSupported { get; }

	/// <summary>
	/// Whether the dialog could appear right now: the app is in the foreground and nothing
	/// (a system prompt, another app) is covering it. Checked before the cooldown starts, so
	/// leaving the app at the wrong moment does not cost the user a request.
	/// </summary>
	Task<bool> CanRequestNowAsync(CancellationToken cancellationToken);

	/// <summary>
	/// Asks the OS to show its rating dialog. The OS decides whether it actually appears and
	/// does not say, so completing normally does not mean the user saw anything.
	/// </summary>
	Task RequestReviewAsync(CancellationToken cancellationToken);
}
