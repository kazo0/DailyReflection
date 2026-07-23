#if !(__ANDROID__ || __IOS__)
namespace DailyReflection.PlatformServices;

public partial class VersionTrackingService
{
	// Desktop fallback. Returns nulls so the assembly-metadata path in
	// VersionTrackingService.cs runs.
	private partial (string? version, string? build) ReadNativeVersion()
		=> (null, null);
}
#endif
