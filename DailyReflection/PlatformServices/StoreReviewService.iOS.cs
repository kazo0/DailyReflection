#if __IOS__
using StoreKit;
using UIKit;

namespace DailyReflection.PlatformServices;

/// <summary>
/// StoreKit rating dialog (Spec 012 §C). <c>SKStoreReviewController.RequestReview(UIWindowScene)</c>
/// is the scene-based overload (iOS 14+; the app's minimum is 15). iOS shows the dialog every
/// time in a debug build, never in TestFlight, and at most three times a year in production.
/// </summary>
public partial class StoreReviewService
{
	private static partial bool IsNativeSupported() => true;

	private static partial Task<bool> CanRequestNativeNowAsync(CancellationToken cancellationToken)
	{
		var canRequest = false;
		UIApplication.SharedApplication.InvokeOnMainThread(() => canRequest = GetForegroundScene() is not null);
		return Task.FromResult(canRequest);
	}

	private static partial Task RequestNativeReviewAsync(CancellationToken cancellationToken)
	{
		UIApplication.SharedApplication.InvokeOnMainThread(() =>
		{
			if (GetForegroundScene() is { } scene && !cancellationToken.IsCancellationRequested)
			{
#pragma warning disable CA1422 // Its iOS 18 replacement, StoreKit's AppStore.requestReview(in:), is Swift-only and has no .NET binding
				SKStoreReviewController.RequestReview(scene);
#pragma warning restore CA1422
			}
		});

		return Task.CompletedTask;
	}

	// Only a scene the user is looking at; iOS ignores the request otherwise. Main thread only.
	private static UIWindowScene? GetForegroundScene()
		=> UIApplication.SharedApplication.ConnectedScenes
			.OfType<UIWindowScene>()
			.FirstOrDefault(s => s.ActivationState == UISceneActivationState.ForegroundActive);
}
#endif
