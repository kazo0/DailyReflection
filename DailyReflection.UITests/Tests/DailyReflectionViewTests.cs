using DailyReflection.UITests.Views;
using NUnit.Framework;
using Xamarin.UITest;

namespace DailyReflection.UITests.Tests
{
	public class DailyReflectionViewTests : TestBase<DailyReflectionView>
	{
		public DailyReflectionViewTests(Platform platform) : base(platform)
		{
		}

        [Test]
        public void ReflectionIsDisplayed()
        {
           

            var title = ViewUnderTest.GetReflectionTitle();
            var quote = ViewUnderTest.GetReflectionQuote();
            var quoteSource = ViewUnderTest.GetReflectionQuoteSource();
            var thought = ViewUnderTest.GetReflectionThought();
            var copyright = ViewUnderTest.GetReflectionCopyright();

            Assert.IsFalse(string.IsNullOrEmpty(title));
            Assert.IsFalse(string.IsNullOrEmpty(quote));
            Assert.IsFalse(string.IsNullOrEmpty(quoteSource));
            Assert.IsFalse(string.IsNullOrEmpty(thought));
            Assert.IsFalse(string.IsNullOrEmpty(copyright));
        }

        [Test]
        public void ShareReflection()
        {
            ViewUnderTest.ShareReflection();
        }

		public override void BeforeEachTest()
		{
			base.BeforeEachTest();

            ViewUnderTest.WaitForReflectionContent();
        }
	}
}
