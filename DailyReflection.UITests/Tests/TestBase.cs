using DailyReflection.UITests.Views;
using NUnit.Framework;
using System;
using Xamarin.UITest;

namespace DailyReflection.UITests.Tests
{
	[TestFixture(Platform.Android)]
	[TestFixture(Platform.iOS)]
	public abstract class TestBase<TView> where TView : ViewBase
	{
		protected IApp App { get; private set; }
		protected TView ViewUnderTest { get; private set; }

		private readonly Platform _platform;
		
		public TestBase(Platform platform)
		{
			_platform = platform;
		}

		[SetUp]
		public void BeforeEachTest()
		{
			App = AppInitializer
				.StartApp(_platform);

			ViewUnderTest = (TView)Activator.CreateInstance(typeof(TView), App);

			ViewUnderTest.WaitForViewToLoad();
		}
	}
}
