namespace DailyReflection.Services.Share
{
	public interface IShareService
	{
		Task ShareText(string title, string body);
	}

	public class ShareService : IShareService
	{
		private readonly IShare _share;

		public ShareService(IShare share)
		{
			_share = share;
		}

		public Task ShareText(string title, string body)
		{
			return _share.RequestAsync(text: body, title: title);
		}
	}
}
