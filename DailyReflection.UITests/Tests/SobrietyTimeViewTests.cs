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
	public class SobrietyTimeViewTests : TestBase<SobrietyTimeView>
	{
		public SobrietyTimeViewTests(Platform platform) : base(platform)
		{
		}

		[Test]
		public void EncouragementIsDisplayed()
		{
			var encouragement = ViewUnderTest.GetEncouragementText();

			Assert.AreEqual("One day at a time", encouragement);
		}
	}
}
