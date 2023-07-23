using DailyReflection.Constants;
using MauiPref = Microsoft.Maui.Storage.Preferences;

namespace DailyReflection.Services.Settings
{
	public interface ISettingsService
	{
		T Get<T>(string key, T defaultValue);
		void Set<T>(string key, T value);
		void MigrateOldPreferences();
	}

	public class SettingsService : ISettingsService
	{
		private readonly IReadOnlyDictionary<Type, Func<string, object, object>> _getDict;

		private readonly IReadOnlyDictionary<Type, Action<string, object>> _setDict;

		public SettingsService()
		{
			_getDict =
				new Dictionary<Type, Func<string, object, object>>
				{
					{typeof(bool), (key, defaultValue) => MauiPref.Get(key, (bool)defaultValue, PreferenceConstants.PreferenceSharedName)},
					{typeof(int), (key, defaultValue) => MauiPref.Get(key, (int)defaultValue, PreferenceConstants.PreferenceSharedName)},
					{typeof(double), (key, defaultValue) => MauiPref.Get(key, (double)defaultValue, PreferenceConstants.PreferenceSharedName)},
					{typeof(float), (key, defaultValue) => MauiPref.Get(key, (float)defaultValue, PreferenceConstants.PreferenceSharedName)},
					{typeof(long), (key, defaultValue) => MauiPref.Get(key, (long)defaultValue, PreferenceConstants.PreferenceSharedName)},
					{typeof(string), (key, defaultValue) => MauiPref.Get(key, (string)defaultValue, PreferenceConstants.PreferenceSharedName)},
					{typeof(DateTime), (key, defaultValue) => MauiPref.Get(key, (DateTime)defaultValue, PreferenceConstants.PreferenceSharedName)},
				};

			_setDict =
				new Dictionary<Type, Action<string, object>>
				{
					{typeof(bool), (key, value) => MauiPref.Set(key, (bool)value, PreferenceConstants.PreferenceSharedName)},
					{typeof(int), (key, value) => MauiPref.Set(key, (int)value, PreferenceConstants.PreferenceSharedName)},
					{typeof(double), (key, value) => MauiPref.Set(key, (double)value, PreferenceConstants.PreferenceSharedName)},
					{typeof(float), (key, value) => MauiPref.Set(key, (float)value, PreferenceConstants.PreferenceSharedName)},
					{typeof(long), (key, value) => MauiPref.Set(key, (long)value, PreferenceConstants.PreferenceSharedName)},
					{typeof(string), (key, value) => MauiPref.Set(key, (string)value, PreferenceConstants.PreferenceSharedName)},
					{typeof(DateTime), (key, value) => MauiPref.Set(key, (DateTime)value, PreferenceConstants.PreferenceSharedName)},
				};
		}

		public T Get<T>(string key, T defaultValue)
		{
			if (_getDict.TryGetValue(typeof(T), out var getter))
			{
				return (T)getter.Invoke(key, defaultValue);
			}

			throw new InvalidOperationException("Invalid type for preferences. Must be either a bool, int, double, float, long, string, or DateTime");
		}

		public void Set<T>(string key, T value)
		{
			if (_setDict.TryGetValue(typeof(T), out var setter))
			{
				setter.Invoke(key, value);
				return;
			}

			throw new InvalidOperationException("Invalid type for preferences. Must be either a bool, int, double, float, long, string, or DateTime");
		}

		public void MigrateOldPreferences()
		{
			var soberDate = MauiPref.Get(PreferenceConstants.SoberDate, DateTime.Today);
			var notifsEnabled = MauiPref.Get(PreferenceConstants.NotificationsEnabled, false);
			var notifTime = MauiPref.Get(PreferenceConstants.NotificationTime, DateTime.MinValue);

			Set(PreferenceConstants.SoberDate, soberDate);
			Set(PreferenceConstants.NotificationsEnabled, notifsEnabled);
			Set(PreferenceConstants.NotificationTime, notifTime);

			MauiPref.Clear();
		}
	}
}
