#if __IOS__
using DailyReflection.Core.Constants;
using Foundation;

namespace DailyReflection.PlatformServices;

public partial class SettingsService
{
    partial void ImportLegacyPreferences()
    {
        // Xamarin.Forms builds (>= 2.0/20) stored all settings in the
        // "DR_Settings" NSUserDefaults suite; earlier builds used the standard
        // defaults and were migrated by the old app itself. Both survive an
        // in-place upgrade, so import anything we find — DR_Settings last so
        // its values win.
        ImportLegacyPreferencesFrom(NSUserDefaults.StandardUserDefaults);
        ImportLegacyPreferencesFrom(new NSUserDefaults(PreferenceConstants.PreferenceSharedName, NSUserDefaultsType.SuiteName));
    }

    private void ImportLegacyPreferencesFrom(NSUserDefaults? defaults)
    {
        if (defaults is null)
        {
            return;
        }

        ImportDateTime(defaults, PreferenceConstants.SoberDate);
        ImportDateTime(defaults, PreferenceConstants.NotificationTime);
        ImportBool(defaults, PreferenceConstants.NotificationsEnabled);
        ImportBool(defaults, PreferenceConstants.NotificationRequiresManualAuth);
        ImportInt(defaults, PreferenceConstants.SoberTimeDisplay);
    }

    private void ImportDateTime(NSUserDefaults defaults, string key)
    {
        // Xamarin.Essentials stores DateTime as the stringified ToBinary() long.
        var raw = defaults.StringForKey(key);
        if (raw is null)
        {
            return;
        }

        if (long.TryParse(raw, out var binary))
        {
            Set(key, DateTime.FromBinary(binary));
        }
        else if (DateTime.TryParse(raw, out var parsed))
        {
            Set(key, parsed);
        }
    }

    private void ImportBool(NSUserDefaults defaults, string key)
    {
        if (defaults.ObjectForKey(key) != null)
        {
            Set(key, defaults.BoolForKey(key));
        }
    }

    private void ImportInt(NSUserDefaults defaults, string key)
    {
        if (defaults.ObjectForKey(key) != null)
        {
            Set(key, (int)defaults.IntForKey(key));
        }
    }
}
#endif
