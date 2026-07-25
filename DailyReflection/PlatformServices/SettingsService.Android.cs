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
        // an in-place upgrade, so import anything we find — DR_Settings last so
        // its values win. Xamarin.Essentials encodings: bool/int native,
        // DateTime as ToBinary() long.
        ImportLegacyPreferencesFrom(Android.Preferences.PreferenceManager.GetDefaultSharedPreferences(AndroidApplication.Context));
        ImportLegacyPreferencesFrom(AndroidApplication.Context.GetSharedPreferences(
            PreferenceConstants.PreferenceSharedName,
            FileCreationMode.Private));
    }

    private void ImportLegacyPreferencesFrom(ISharedPreferences? prefs)
    {
        if (prefs == null)
        {
            return;
        }

        if (prefs.Contains(PreferenceConstants.SoberDate))
        {
            Set(PreferenceConstants.SoberDate, DateTime.FromBinary(prefs.GetLong(PreferenceConstants.SoberDate, 0)));
        }

        if (prefs.Contains(PreferenceConstants.NotificationTime))
        {
            Set(PreferenceConstants.NotificationTime, DateTime.FromBinary(prefs.GetLong(PreferenceConstants.NotificationTime, 0)));
        }

        if (prefs.Contains(PreferenceConstants.NotificationsEnabled))
        {
            Set(PreferenceConstants.NotificationsEnabled, prefs.GetBoolean(PreferenceConstants.NotificationsEnabled, false));
        }

        if (prefs.Contains(PreferenceConstants.NotificationRequiresManualAuth))
        {
            Set(PreferenceConstants.NotificationRequiresManualAuth, prefs.GetBoolean(PreferenceConstants.NotificationRequiresManualAuth, false));
        }

        if (prefs.Contains(PreferenceConstants.SoberTimeDisplay))
        {
            Set(PreferenceConstants.SoberTimeDisplay, prefs.GetInt(PreferenceConstants.SoberTimeDisplay, 0));
        }
    }
}
#endif
