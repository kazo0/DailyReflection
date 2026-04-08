using System.Threading.Tasks;


namespace DailyReflection.Services.Share;

public interface IShareService
{
	Task ShareText(string title, string body);
}
