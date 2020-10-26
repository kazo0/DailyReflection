using DailyReflection.UITests.Views;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.UITest;

namespace DailyReflection.UITests.Tests
{
	public class SettingsViewTests : TestBase<SettingsView>
	{
		public SettingsViewTests(Platform platform) : base(platform)
		{
		}

		[Test]
		public void TimePickerStartsDisabled()
		{
			Assert.IsFalse(ViewUnderTest.IsTimePickerEnabled());
		}

		[Test]
		public void EnablingNotificationEnablesTimePicker()
		{
			ViewUnderTest.EnableNotifications();
			Assert.IsTrue(ViewUnderTest.IsTimePickerEnabled());
		}

		[Test]
		public void TimePickerIsDisplayed()
		{
			ViewUnderTest.EnableNotifications();
			ViewUnderTest.OpenTimePicker();

			Assert.IsTrue(ViewUnderTest.IsTimePickerOpen());
		}

		[Test]
		public void DatePickerIsDisplayed()
		{
			ViewUnderTest.OpenDatePicker();

			Assert.IsTrue(ViewUnderTest.IsDatePickerOpen());
		}
	}
}
