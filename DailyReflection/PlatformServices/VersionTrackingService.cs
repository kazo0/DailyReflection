using DailyReflection.Services.VersionTracking;
using System.Reflection;
using Windows.Storage;

namespace DailyReflection.PlatformServices;

/// <summary>
/// LocalSettings-backed port of Xamarin.Essentials VersionTracking.
/// Platform-specific source for current version/build is provided via partial methods
/// in <c>VersionTrackingService.{Android,iOS}.cs</c>; the desktop fallback reads the
/// running assembly's informational/file version.
/// </summary>
public partial class VersionTrackingService : IVersionTrackingService
{
	private const string KeyVersion  = "DR_VT_Version";
	private const string KeyBuild    = "DR_VT_Build";
	private const string KeyPrevVer  = "DR_VT_PrevVersion";
	private const string KeyPrevBld  = "DR_VT_PrevBuild";

	private readonly ApplicationDataContainer _store = ApplicationData.Current.LocalSettings;

	public string CurrentVersion { get; }
	public string CurrentBuild { get; }

	public bool IsFirstLaunchEver { get; private set; }
	public bool IsFirstLaunchForCurrentVersion { get; private set; }
	public bool IsFirstLaunchForCurrentBuild { get; private set; }
	public string? PreviousVersion { get; private set; }
	public string? PreviousBuild { get; private set; }

	private bool _tracked;

	public VersionTrackingService()
	{
		(CurrentVersion, CurrentBuild) = ReadPlatformVersion();
	}

	public void Track()
	{
		if (_tracked) return;
		_tracked = true;

		var storedVersion = _store.Values[KeyVersion] as string;
		var storedBuild   = _store.Values[KeyBuild]   as string;

		IsFirstLaunchEver = storedVersion is null && storedBuild is null;

		IsFirstLaunchForCurrentVersion = storedVersion != CurrentVersion;
		if (IsFirstLaunchForCurrentVersion)
		{
			PreviousVersion = storedVersion;
			_store.Values[KeyPrevVer] = storedVersion;
			_store.Values[KeyVersion] = CurrentVersion;
		}
		else
		{
			PreviousVersion = _store.Values[KeyPrevVer] as string;
		}

		IsFirstLaunchForCurrentBuild = storedBuild != CurrentBuild;
		if (IsFirstLaunchForCurrentBuild)
		{
			PreviousBuild = storedBuild;
			_store.Values[KeyPrevBld] = storedBuild;
			_store.Values[KeyBuild]   = CurrentBuild;
		}
		else
		{
			PreviousBuild = _store.Values[KeyPrevBld] as string;
		}
	}

	private (string version, string build) ReadPlatformVersion()
	{
		var (version, build) = ReadNativeVersion();
		if (!string.IsNullOrEmpty(version) && !string.IsNullOrEmpty(build))
			return (version!, build!);

		// Desktop fallback — read from the entry assembly metadata.
		var asm = Assembly.GetEntryAssembly() ?? GetType().Assembly;
		var info = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
		var file = asm.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version;
		var name = asm.GetName().Version;

		var fallbackVersion = info ?? name?.ToString(3) ?? "1.0";
		var fallbackBuild   = file ?? name?.Build.ToString() ?? "1";

		return (fallbackVersion, fallbackBuild);
	}

	// Implemented as partial methods on Android / iOS; on desktop the partial
	// returns (null, null) and ReadPlatformVersion falls through to the assembly path.
	private partial (string? version, string? build) ReadNativeVersion();
}
