namespace DailyReflection.Services.VersionTracking;

/// <summary>
/// Replicates the surface of <c>Xamarin.Essentials.VersionTracking</c> needed by the
/// startup migrations. Implemented per-head against the platform's package metadata.
/// </summary>
public interface IVersionTrackingService
{
	/// <summary>
	/// Records the current version/build as "seen" so subsequent launches can detect
	/// version changes. Must be called once per launch before reading any of the flags.
	/// </summary>
	void Track();

	bool IsFirstLaunchEver { get; }
	bool IsFirstLaunchForCurrentVersion { get; }
	bool IsFirstLaunchForCurrentBuild { get; }

	string CurrentVersion { get; }
	string? PreviousVersion { get; }

	string CurrentBuild { get; }
	string? PreviousBuild { get; }
}
