#if __WASM__
namespace DailyReflection.PlatformServices;

public partial class StoreReviewService
{
	// The web app has no store listing to rate, same as desktop.
	private static partial bool IsNativeSupported() => false;

	private static partial Task<bool> CanRequestNativeNowAsync(CancellationToken cancellationToken)
		=> Task.FromResult(false);

	private static partial Task RequestNativeReviewAsync(CancellationToken cancellationToken)
		=> Task.CompletedTask;
}
#endif
