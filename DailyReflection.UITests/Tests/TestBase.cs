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
		private IApp _app;
		protected TView ViewUnderTest { get; private set; }

		private readonly Platform _platform;
		
		public TestBase(Platform platform)
		{
			_platform = platform;
			
		}

		[SetUp]
		public virtual void BeforeEachTest()
		{
			_app = AppInitializer
				.StartApp(_platform);

			ViewUnderTest = (TView)Activator.CreateInstance(typeof(TView), _app, _platform);
			ViewUnderTest.WaitForViewToLoad();
		}
	}
}
