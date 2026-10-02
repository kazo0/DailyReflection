#if !(__ANDROID__ || __IOS__ || __WASM__)
namespace DailyReflection.PlatformServices;

public partial class StoreReviewService
{
	// Desktop has no in-app rating dialog (the zips are not store-distributed).
	private static partial bool IsNativeSupported() => false;

	private static partial Task<bool> CanRequestNativeNowAsync(CancellationToken cancellationToken)
		=> Task.FromResult(false);

	private static partial Task RequestNativeReviewAsync(CancellationToken cancellationToken)
		=> Task.CompletedTask;
}
#endif
