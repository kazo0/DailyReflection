using DailyReflection.Services.Review;

namespace DailyReflection.PlatformServices;

/// <summary>
/// The platform's in-app rating dialog (Spec 012 §C). Implemented by partial methods in
/// <c>StoreReviewService.{Android,iOS}.cs</c>; the desktop and WebAssembly partials report
/// it unsupported, and <see cref="ReviewPromptService"/> then does nothing at all.
/// </summary>
public partial class StoreReviewService : IStoreReviewService
{
	public bool IsSupported => IsNativeSupported();

	public Task<bool> CanRequestNowAsync(CancellationToken cancellationToken)
		=> CanRequestNativeNowAsync(cancellationToken);

	public Task RequestReviewAsync(CancellationToken cancellationToken)
		=> RequestNativeReviewAsync(cancellationToken);

	private static partial bool IsNativeSupported();

	private static partial Task<bool> CanRequestNativeNowAsync(CancellationToken cancellationToken);

	private static partial Task RequestNativeReviewAsync(CancellationToken cancellationToken);
}
