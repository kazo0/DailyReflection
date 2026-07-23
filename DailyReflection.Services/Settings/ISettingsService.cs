namespace DailyReflection.Services.Settings;

public interface ISettingsService
{
	T Get<T>(string key, T defaultValue);
	void Set<T>(string key, T value);
	void MigrateOldPreferences();
}
