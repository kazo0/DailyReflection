#if __ANDROID__
using Android.Content.PM;
using AndroidApplication = Android.App.Application;

namespace DailyReflection.PlatformServices;

public partial class VersionTrackingService
{
	private partial (string? version, string? build) ReadNativeVersion()
	{
		try
		{
			var ctx = AndroidApplication.Context;
			var pm = ctx.PackageManager;
			if (pm == null)
			{
				return (null, null);
			}

#pragma warning disable CA1416 // PackageManager.GetPackageInfo overloads exist on every supported API level
			var info = pm.GetPackageInfo(ctx.PackageName!, PackageInfoFlags.Activities);
#pragma warning restore CA1416
			if (info == null)
			{
				return (null, null);
			}

			var version = info.VersionName ?? "1.0";
			var build = (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.P
				? info.LongVersionCode
				: info.VersionCode).ToString();
			return (version, build);
		}
		catch
		{
			return (null, null);
		}
	}
}
#endif
