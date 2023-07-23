using MauiShare = Microsoft.Maui.ApplicationModel.DataTransfer.Share;

namespace DailyReflection.Services.Share
{
	public interface IShareService
	{
		Task ShareText(string title, string body);
	}

	public class ShareService : IShareService
	{
		public ShareService()
		{
		}

		public Task ShareText(string title, string body)
		{
			return MauiShare.RequestAsync(text: body, title: title);
		}
	}
}
