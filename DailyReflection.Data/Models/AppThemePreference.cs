namespace DailyReflection.Data.Models;

/// <summary>
/// The user's app theme choice (Settings → Display → Theme). Persisted as its
/// <c>int</c> value under <c>PreferenceConstants.AppThemePreference</c>, so
/// the numeric values are part of the stored contract — append, never renumber.
/// </summary>
public enum AppThemePreference
{
	/// <summary>Follow the operating system's light/dark setting (default).</summary>
	System = 0,
	Light = 1,
	Dark = 2,
}
