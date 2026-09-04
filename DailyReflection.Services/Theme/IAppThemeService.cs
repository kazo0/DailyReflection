using DailyReflection.Data.Models;

namespace DailyReflection.Services.Theme;

/// <summary>
/// Applies the user's <see cref="AppThemePreference"/> to the running UI.
/// Implemented in the Uno head (<c>PlatformServices/AppThemeService</c>), which
/// owns the window; the shared layers only decide <em>which</em> theme applies.
/// </summary>
public interface IAppThemeService
{
	/// <summary>
	/// Switches the app to <paramref name="preference"/> — light, dark, or
	/// following the OS. Safe to call from any thread; no-op before the main
	/// window has content.
	/// </summary>
	void ApplyTheme(AppThemePreference preference);
}
