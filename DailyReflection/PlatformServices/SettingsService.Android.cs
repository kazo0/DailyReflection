#if __ANDROID__
using Android.Content;
using DailyReflection.Core.Constants;
using AndroidApplication = Android.App.Application;

namespace DailyReflection.PlatformServices;

public partial class SettingsService
{
	// Mirror writes to Android SharedPreferences so background components
	// (e.g. DailyNotificationReceiver) can read the same values without
	// depending on Windows.Storage.ApplicationData inside a BroadcastReceiver.
	partial void MirrorSet<T>(string key, T value)
	{
		var prefs = AndroidApplication.Context.GetSharedPreferences(
			PreferenceConstants.PreferenceSharedName,
			FileCreationMode.Private);

		if (prefs == null)
		{
			return;
		}

		ISharedPreferencesEditor editor = prefs.Edit()!;

		switch (value)
		{
			case DateTime dt:
				editor.PutLong(key, dt.ToBinary());
				break;
			case bool b:
				editor.PutBoolean(key, b);
				break;
			case int i:
				editor.PutInt(key, i);
				break;
			case long l:
				editor.PutLong(key, l);
				break;
			case float f:
				editor.PutFloat(key, f);
				break;
			case string s:
				editor.PutString(key, s);
				break;
			case null:
				editor.Remove(key);
				break;
			default:
				editor.PutString(key, value!.ToString());
				break;
		}

		editor.Apply();
	}

	partial void ImportLegacyPreferences()
	{
		// Xamarin.Forms builds (>= 2.0/20) stored all settings in the
		// "DR_Settings" SharedPreferences file; earlier builds used the default
		// container and were migrated by the old app itself. Both files survive
		// an in-place upgrade, and DR_Settings holds the current values, so it is
		// imported first and the default container only fills keys DR_Settings
		// couldn't supply. The order matters: Set mirrors every value into
		// DR_Settings (MirrorSet), so importing the default container first
		// overwrote the current DR_Settings values with stale pre-2.0 ones before
		// they were read. Xamarin.Essentials encodings: bool/int native, DateTime
		// as ToBinary() long.
		var imported = ImportLegacyPreferencesFrom(
			AndroidApplication.Context.GetSharedPreferences(PreferenceConstants.PreferenceSharedName, FileCreationMode.Private),
			alreadyImported: new HashSet<string>());
		ImportLegacyPreferencesFrom(
			Android.Preferences.PreferenceManager.GetDefaultSharedPreferences(AndroidApplication.Context),
			alreadyImported: imported);
	}

	private HashSet<string> ImportLegacyPreferencesFrom(ISharedPreferences? prefs, IReadOnlySet<string> alreadyImported)
	{
		var imported = new HashSet<string>();
		if (prefs == null)
		{
			return imported;
		}

		void Import(string key, Action import)
		{
			if (!alreadyImported.Contains(key) && ImportKey(prefs, key, import))
			{
				imported.Add(key);
			}
		}

		Import(PreferenceConstants.SoberDate, () => Set(PreferenceConstants.SoberDate, DateTime.FromBinary(prefs.GetLong(PreferenceConstants.SoberDate, 0))));
		Import(PreferenceConstants.NotificationTime, () => Set(PreferenceConstants.NotificationTime, DateTime.FromBinary(prefs.GetLong(PreferenceConstants.NotificationTime, 0))));
		Import(PreferenceConstants.NotificationsEnabled, () => Set(PreferenceConstants.NotificationsEnabled, prefs.GetBoolean(PreferenceConstants.NotificationsEnabled, false)));
		Import(PreferenceConstants.NotificationRequiresManualAuth, () => Set(PreferenceConstants.NotificationRequiresManualAuth, prefs.GetBoolean(PreferenceConstants.NotificationRequiresManualAuth, false)));
		Import(PreferenceConstants.SoberTimeDisplay, () => Set(PreferenceConstants.SoberTimeDisplay, prefs.GetInt(PreferenceConstants.SoberTimeDisplay, 0)));
		return imported;
	}

	private bool ImportKey(ISharedPreferences prefs, string key, Action import)
	{
		if (!prefs.Contains(key))
		{
			return false;
		}

		// A value stored under an unexpected type makes the typed getter throw
		// ClassCastException. Skip that one key rather than abandoning the import,
		// which would lose every setting after it.
		try
		{
			import();
			return true;
		}
		catch (Java.Lang.ClassCastException ex)
		{
			this.Log().LogWarning(ex, "Skipped legacy setting '{Key}' stored with an unexpected type.", key);
			return false;
		}
	}
}
#endif
