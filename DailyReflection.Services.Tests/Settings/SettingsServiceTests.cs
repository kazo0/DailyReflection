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
		public void Get_Throws_For_Invalid_Type()
		{
			Assert.Throws<InvalidOperationException>(() => ServiceUnderTest.Get("testKey", default(Reflection)));
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

		[Test]
		public void Get_Call_Preferences_For_DateTime()
		{
			_preferences.Setup(x => x.Get("testKey", default(DateTime), PreferenceConstants.PreferenceSharedName))
				.Returns(DateTime.Today);

			var setting = ServiceUnderTest.Get("testKey", default(DateTime));

			Assert.AreEqual(DateTime.Today, setting);
			_preferences.Verify(x => x.Get("testKey", DateTime.MinValue, PreferenceConstants.PreferenceSharedName), Times.Once);
		}

		[Test]
		public void Get_Call_Preferences_For_Bool()
		{
			_preferences.Setup(x => x.Get("testKey", false, PreferenceConstants.PreferenceSharedName))
				.Returns(true);

			var setting = ServiceUnderTest.Get("testKey", false);

			Assert.IsTrue(setting);
			_preferences.Verify(x => x.Get("testKey", false, PreferenceConstants.PreferenceSharedName), Times.Once);
		}

		[Test]
		public void Get_Call_Preferences_For_Double()
		{
			_preferences.Setup(x => x.Get("testKey", 0d, PreferenceConstants.PreferenceSharedName))
				.Returns(2.0);

			var setting = ServiceUnderTest.Get("testKey", 0d);

			Assert.AreEqual(2.0d, setting);
			_preferences.Verify(x => x.Get("testKey", 0d, PreferenceConstants.PreferenceSharedName), Times.Once);
		}

		[Test]
		public void Get_Call_Preferences_For_Float()
		{
			_preferences.Setup(x => x.Get("testKey", 0f, PreferenceConstants.PreferenceSharedName))
				.Returns(2.0f);

			var setting = ServiceUnderTest.Get("testKey", 0f);

			Assert.AreEqual(2.0f, setting);
			_preferences.Verify(x => x.Get("testKey", 0f, PreferenceConstants.PreferenceSharedName), Times.Once);
		}

		[Test]
		public void Get_Call_Preferences_For_Long()
		{
			_preferences.Setup(x => x.Get("testKey", 0L, PreferenceConstants.PreferenceSharedName))
				.Returns(2L);

			ServiceUnderTest.Set("testKey", 2L);
			var setting = ServiceUnderTest.Get("testKey", 0L);

			Assert.AreEqual(2L, setting);
			_preferences.Verify(x => x.Get("testKey", 0L, PreferenceConstants.PreferenceSharedName), Times.Once);
		}

		[Test]
		public void Get_Call_Preferences_For_String()
		{
			_preferences.Setup(x => x.Get("testKey", default(string), PreferenceConstants.PreferenceSharedName))
				.Returns("test string");

			var setting = ServiceUnderTest.Get("testKey", default(string));

			Assert.AreEqual("test string", setting);
			_preferences.Verify(x => x.Get("testKey", default(string), PreferenceConstants.PreferenceSharedName), Times.Once);
		}

		[Test]
		public void Get_Call_Preferences_For_Int()
		{
			_preferences.Setup(x => x.Get("testKey", 0, PreferenceConstants.PreferenceSharedName))
				.Returns(2);

			ServiceUnderTest.Set("testKey", 2);
			var setting = ServiceUnderTest.Get("testKey", 0);

			Assert.AreEqual(2, setting);
			_preferences.Verify(x => x.Get("testKey", 0, PreferenceConstants.PreferenceSharedName), Times.Once);
		}

		[Test]
		public void Migration_Old_Preferences()
		{
			_preferences.Setup(x => x.Get(PreferenceConstants.SoberDate, DateTime.Today))
				.Returns(new DateTime(2020, 12, 31));

			_preferences.Setup(x => x.Get(PreferenceConstants.NotificationsEnabled, false))
				.Returns(true);

			_preferences.Setup(x => x.Get(PreferenceConstants.NotificationTime, default(DateTime)))
				.Returns(new DateTime(2020, 12, 31, 9, 0, 0));

			ServiceUnderTest.MigrateOldPreferences();

			_preferences.Verify(x => x.Get(PreferenceConstants.SoberDate, DateTime.Today), Times.Once);
			_preferences.Verify(x => x.Get(PreferenceConstants.NotificationsEnabled, false), Times.Once);
			_preferences.Verify(x => x.Get(PreferenceConstants.NotificationTime, default(DateTime)), Times.Once);

			_preferences.Verify(x => x.Set(PreferenceConstants.SoberDate, new DateTime(2020, 12, 31), PreferenceConstants.PreferenceSharedName), 
				Times.Once);
			_preferences.Verify(x => x.Set(PreferenceConstants.NotificationsEnabled, true, PreferenceConstants.PreferenceSharedName), 
				Times.Once);
			_preferences.Verify(x => x.Set(PreferenceConstants.NotificationTime, new DateTime(2020, 12, 31, 9, 0, 0), PreferenceConstants.PreferenceSharedName), 
				Times.Once);
			_preferences.Verify(x => x.Clear(), Times.Once);
		}
	}
}
