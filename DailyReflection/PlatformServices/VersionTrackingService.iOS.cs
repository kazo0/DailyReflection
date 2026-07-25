#if __IOS__
using Foundation;

namespace DailyReflection.PlatformServices;

public partial class VersionTrackingService
{
	private partial (string? version, string? build) ReadNativeVersion()
	{
		var info = NSBundle.MainBundle.InfoDictionary;
		var version = info?["CFBundleShortVersionString"]?.ToString();
		var build = info?["CFBundleVersion"]?.ToString();
		return (version, build);
	}
}
#endif
