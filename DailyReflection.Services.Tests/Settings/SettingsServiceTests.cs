using DailyReflection.Core.Constants;
using DailyReflection.Data.Models;
using DailyReflection.Services.Settings;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Essentials.Interfaces;

namespace DailyReflection.Services.Tests.Settings
{
	public class SettingsServiceTests : ServiceTestBase<SettingsService>
	{
		private Mock<IPreferences> _preferences;

		protected override SettingsService GetService()
		{
			_preferences = new Mock<IPreferences>();

			return new SettingsService(_preferences.Object);
		}

		[Test]
		public void Set_Throws_For_Invalid_Type()
		{
			Assert.Throws<InvalidOperationException>(() => ServiceUnderTest.Set("testKey", new Reflection()));
		}

		[Test]
		public void Set_Call_Preferences_For_DateTime()
		{
			ServiceUnderTest.Set("testKey", DateTime.Today);

			_preferences.Verify(x => x.Set("testKey", DateTime.Today, PreferenceConstants.PreferenceSharedName), Times.Once);
		}

		[Test]
		public void Set_Call_Preferences_For_Bool()
		{
			ServiceUnderTest.Set("testKey", true);

			_preferences.Verify(x => x.Set("testKey", true, PreferenceConstants.PreferenceSharedName), Times.Once);
		}

		[Test]
		public void Set_Call_Preferences_For_Double()
		{
			ServiceUnderTest.Set("testKey", 2.0d);

			_preferences.Verify(x => x.Set("testKey", 2.0d, PreferenceConstants.PreferenceSharedName), Times.Once);
		}

		[Test]
		public void Set_Call_Preferences_For_Float()
		{
			ServiceUnderTest.Set("testKey", 2.0f);

			_preferences.Verify(x => x.Set("testKey", 2.0f, PreferenceConstants.PreferenceSharedName), Times.Once);
		}

		[Test]
		public void Set_Call_Preferences_For_Long()
		{
			ServiceUnderTest.Set("testKey", 2L);

			_preferences.Verify(x => x.Set("testKey", 2L, PreferenceConstants.PreferenceSharedName), Times.Once);
		}

		[Test]
		public void Set_Call_Preferences_For_String()
		{
			ServiceUnderTest.Set("testKey", "test string");

			_preferences.Verify(x => x.Set("testKey", "test string", PreferenceConstants.PreferenceSharedName), Times.Once);
		}
	}
}
