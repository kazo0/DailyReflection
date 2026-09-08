using System.Threading.Tasks;

namespace DailyReflection.Services.Clipboard;

public interface IClipboardService
{
	/// <summary>Replaces the system clipboard content with <paramref name="text"/>.</summary>
	Task SetTextAsync(string text);
}
