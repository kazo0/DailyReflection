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
}
#endif
