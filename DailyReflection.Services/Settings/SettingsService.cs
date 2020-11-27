using DailyReflection.Core.Constants;
using System;
using System.Collections.Generic;
using Xamarin.Essentials;

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
		private readonly IReadOnlyDictionary<Type, Func<string, object, object>> _getDict =
			new Dictionary<Type, Func<string, object, object>>
			{
				{typeof(bool), (key, defaultValue) => Preferences.Get(key, (bool)defaultValue, PreferenceConstants.PreferenceSharedName)},
				{typeof(double), (key, defaultValue) => Preferences.Get(key, (double)defaultValue, PreferenceConstants.PreferenceSharedName)},
				{typeof(float), (key, defaultValue) => Preferences.Get(key, (float)defaultValue, PreferenceConstants.PreferenceSharedName)},
				{typeof(long), (key, defaultValue) => Preferences.Get(key, (long)defaultValue, PreferenceConstants.PreferenceSharedName)},
				{typeof(string), (key, defaultValue) => Preferences.Get(key, (string)defaultValue, PreferenceConstants.PreferenceSharedName)},
				{typeof(DateTime), (key, defaultValue) => Preferences.Get(key, (DateTime)defaultValue, PreferenceConstants.PreferenceSharedName)},
			};

		private readonly IReadOnlyDictionary<Type, Action<string, object>> _setDict =
			new Dictionary<Type, Action<string, object>>
			{
				{typeof(bool), (key, value) => Preferences.Set(key, (bool)value, PreferenceConstants.PreferenceSharedName)},
				{typeof(double), (key, value) => Preferences.Set(key, (double)value, PreferenceConstants.PreferenceSharedName)},
				{typeof(float), (key, value) => Preferences.Set(key, (float)value, PreferenceConstants.PreferenceSharedName)},
				{typeof(long), (key, value) => Preferences.Set(key, (long)value, PreferenceConstants.PreferenceSharedName)},
				{typeof(string), (key, value) => Preferences.Set(key, (string)value, PreferenceConstants.PreferenceSharedName)},
				{typeof(DateTime), (key, value) => Preferences.Set(key, (DateTime)value, PreferenceConstants.PreferenceSharedName)},
			};


		public T Get<T>(string key, T defaultValue)
		{
			if (_getDict.TryGetValue(typeof(T), out var getter)) 
			{
				return (T)getter.Invoke(key, defaultValue);
			}

			throw new InvalidOperationException("Invalid type for preferences. Must be either a bool, double, float, long, string, or DateTime");
		}

		public void Set<T>(string key, T value)
		{
			if (_setDict.TryGetValue(typeof(T), out var setter))
			{
				setter.Invoke(key, value);
				return;
			}

			throw new InvalidOperationException("Invalid type for preferences. Must be either a bool, double, float, long, string, or DateTime");
		}

		public void MigrateOldPreferences()
		{
			var soberDate = Preferences.Get(PreferenceConstants.SoberDate, DateTime.Today);
			var notifsEnabled = Preferences.Get(PreferenceConstants.NotificationsEnabled, false);
			var notifTime = Preferences.Get(PreferenceConstants.NotificationTime, DateTime.MinValue);

			Set(PreferenceConstants.SoberDate, soberDate);
			Set(PreferenceConstants.NotificationsEnabled, notifsEnabled);
			Set(PreferenceConstants.NotificationTime, notifTime);

			Preferences.Clear();
		}
	}
}
