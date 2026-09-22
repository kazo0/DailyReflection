#if __WASM__
namespace DailyReflection.PlatformServices;

public partial class VersionTrackingService
{
	// WebAssembly fallback, same as desktop. Returns nulls so the
	// assembly-metadata path in VersionTrackingService.cs runs.
	private partial (string? version, string? build) ReadNativeVersion()
		=> (null, null);
}
#endif
