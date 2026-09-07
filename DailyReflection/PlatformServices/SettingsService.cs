using DailyReflection.Services.Settings;

namespace DailyReflection.PlatformServices;

/// <summary>
/// Settings service implementation using Uno Platform's ApplicationData.LocalSettings
/// Based on Uno Platform Docs: features/settings.md
/// Maps from MAUI Preferences to WinUI/Uno ApplicationData.LocalSettings
/// </summary>
public partial class SettingsService : ISettingsService
{
	private readonly ApplicationDataContainer _localSettings;

	public SettingsService()
	{
		_localSettings = ApplicationData.Current.LocalSettings;
	}

	public T Get<T>(string key, T defaultValue)
	{
		if (_localSettings.Values.TryGetValue(key, out var value))
		{
			if (value is T typedValue)
			{
				return typedValue;
			}

			// Handle DateTime stored as string (binary)
			if (typeof(T) == typeof(DateTime) && value is long longValue)
			{
				return (T)(object)DateTime.FromBinary(longValue);
			}

			// Handle DateTime stored as string
			if (typeof(T) == typeof(DateTime) && value is string stringValue)
			{
				if (DateTime.TryParse(stringValue, out var dateTime))
				{
					return (T)(object)dateTime;
				}
			}

			// Try convert
			try
			{
				return (T)Convert.ChangeType(value, typeof(T));
			}
			catch
			{
				return defaultValue;
			}
		}
		return defaultValue;
	}

	public void Set<T>(string key, T value)
	{
		if (value is DateTime dateTime)
		{
			// Store DateTime as binary long value
			_localSettings.Values[key] = dateTime.ToBinary();
		}
		else
		{
			_localSettings.Values[key] = value;
		}

		MirrorSet(key, value);
	}

	public void MigrateOldPreferences()
	{
		// In-place upgrade from the Xamarin.Forms app: pull any values the old
		// app left in the platform stores (Android SharedPreferences / iOS
		// NSUserDefaults) into LocalSettings. No-op on fresh installs and on
		// platforms without a legacy store (desktop).
		ImportLegacyPreferences();
	}

	partial void ImportLegacyPreferences();

	partial void MirrorSet<T>(string key, T value);
}
