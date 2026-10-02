#if __ANDROID__
using Android.Gms.Extensions;
using Xamarin.Google.Android.Play.Core.Review;

namespace DailyReflection.PlatformServices;

/// <summary>
/// Google Play In-App Review API (Spec 012 §C), from the
/// <c>Xamarin.Google.Android.Play.Review</c> binding of <c>com.google.android.play:review</c>.
/// Play only shows the dialog to an app installed from Play (including the internal test
/// track and internal app sharing) and enforces its own quota; when it declines, the launch
/// flow still completes successfully and nothing appears.
/// </summary>
public partial class StoreReviewService
{
	private static partial bool IsNativeSupported() => true;

	// Not over a system dialog or permission prompt, and not after the user has left the app.
	private static async partial Task<bool> CanRequestNativeNowAsync(CancellationToken cancellationToken)
	{
		var activity = await global::Uno.UI.BaseActivity.GetCurrent(cancellationToken);
		return activity.HasWindowFocus;
	}

	private static async partial Task RequestNativeReviewAsync(CancellationToken cancellationToken)
	{
		var activity = await global::Uno.UI.BaseActivity.GetCurrent(cancellationToken);
		var manager = ReviewManagerFactory.Create(activity);
		var reviewInfo = await manager.RequestReviewFlow().AsAsync<ReviewInfo>();
		cancellationToken.ThrowIfCancellationRequested();
		await manager.LaunchReviewFlow(activity, reviewInfo).AsAsync();
	}
}
#endif
