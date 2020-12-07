using DailyReflection.Services.Share;
using Moq;
using NUnit.Framework;
using Xamarin.Essentials.Interfaces;

namespace DailyReflection.Services.Tests.Share
{
	public class ShareServiceTests : ServiceTestBase<ShareService>
	{
		private Mock<IShare> _share;

		protected override ShareService GetService()
		{
			_share = new Mock<IShare>();

			return new ShareService(_share.Object);
		}

		[Test]
		public void Calling_Share_Request_A_Share()
		{
			ServiceUnderTest.ShareText("Test", "test body");

			_share.Verify(x => x.RequestAsync("test body", "Test"), Times.Once);
		}
	}
}
