namespace DailyReflection.Data.Models;

/// <summary>
/// The sober time display selected in Settings. Lives beside
/// <see cref="SoberTimeDisplayPreference"/> because DailyReflection.Core
/// cannot reference this project.
/// </summary>
public record SoberTimeDisplaySelection(SoberTimeDisplayPreference Preference);
