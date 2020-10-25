using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Xamarin.UITest;
using Xamarin.UITest.Queries;

namespace DailyReflection.UITests
{
    [TestFixture(Platform.Android)]
    [TestFixture(Platform.iOS)]
    public class Tests
    {
        private IApp _app;
        private Platform _platform;

        public Tests(Platform platform)
        {
            _platform = platform;
        }

        [SetUp]
        public void BeforeEachTest()
        {
            _app = AppInitializer
                .StartApp(_platform);
        }

        [Test]
        public void ReflectionIsDisplayed()
        {
            _app.WaitForElement(c => c.Marked("reflection_title"));

            var quote = _app.Query(x => x.Marked("reflection_quote"));
            var quoteSource = _app.Query(x => x.Marked("reflection_quoteSource"));
            var thought = _app.Query(x => x.Marked("reflection_thought"));
            var copyright = _app.Query(x => x.Marked("reflection_copyright"));

            Assert.IsTrue(!string.IsNullOrEmpty(quote.FirstOrDefault()?.Text));
            Assert.IsTrue(!string.IsNullOrEmpty(quoteSource.FirstOrDefault()?.Text));
            Assert.IsTrue(!string.IsNullOrEmpty(thought.FirstOrDefault()?.Text));
            Assert.IsTrue(!string.IsNullOrEmpty(copyright.FirstOrDefault()?.Text));
        }
    }
}
