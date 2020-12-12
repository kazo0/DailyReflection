using DailyReflection.Core.Constants;
using System;
using System.Collections.Generic;
using Xamarin.Essentials.Interfaces;

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

		private readonly IPreferences _preferences;

		public SettingsService(IPreferences preferences)
		{
			_preferences = preferences;

			_getDict =
				new Dictionary<Type, Func<string, object, object>>
				{
					{typeof(bool), (key, defaultValue) => _preferences.Get(key, (bool)defaultValue, PreferenceConstants.PreferenceSharedName)},
					{typeof(int), (key, defaultValue) => _preferences.Get(key, (int)defaultValue, PreferenceConstants.PreferenceSharedName)},
					{typeof(double), (key, defaultValue) => _preferences.Get(key, (double)defaultValue, PreferenceConstants.PreferenceSharedName)},
					{typeof(float), (key, defaultValue) => _preferences.Get(key, (float)defaultValue, PreferenceConstants.PreferenceSharedName)},
					{typeof(long), (key, defaultValue) => _preferences.Get(key, (long)defaultValue, PreferenceConstants.PreferenceSharedName)},
					{typeof(string), (key, defaultValue) => _preferences.Get(key, (string)defaultValue, PreferenceConstants.PreferenceSharedName)},
					{typeof(DateTime), (key, defaultValue) => _preferences.Get(key, (DateTime)defaultValue, PreferenceConstants.PreferenceSharedName)},
				};

			_setDict =
				new Dictionary<Type, Action<string, object>>
				{
					{typeof(bool), (key, value) => _preferences.Set(key, (bool)value, PreferenceConstants.PreferenceSharedName)},
					{typeof(int), (key, value) => _preferences.Set(key, (int)value, PreferenceConstants.PreferenceSharedName)},
					{typeof(double), (key, value) => _preferences.Set(key, (double)value, PreferenceConstants.PreferenceSharedName)},
					{typeof(float), (key, value) => _preferences.Set(key, (float)value, PreferenceConstants.PreferenceSharedName)},
					{typeof(long), (key, value) => _preferences.Set(key, (long)value, PreferenceConstants.PreferenceSharedName)},
					{typeof(string), (key, value) => _preferences.Set(key, (string)value, PreferenceConstants.PreferenceSharedName)},
					{typeof(DateTime), (key, value) => _preferences.Set(key, (DateTime)value, PreferenceConstants.PreferenceSharedName)},
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
			var soberDate = _preferences.Get(PreferenceConstants.SoberDate, DateTime.Today);
			var notifsEnabled = _preferences.Get(PreferenceConstants.NotificationsEnabled, false);
			var notifTime = _preferences.Get(PreferenceConstants.NotificationTime, DateTime.MinValue);

			Set(PreferenceConstants.SoberDate, soberDate);
			Set(PreferenceConstants.NotificationsEnabled, notifsEnabled);
			Set(PreferenceConstants.NotificationTime, notifTime);

			_preferences.Clear();
		}
	}
}
