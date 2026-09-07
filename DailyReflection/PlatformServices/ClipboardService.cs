using DailyReflection.Services.Clipboard;
using Windows.ApplicationModel.DataTransfer;

namespace DailyReflection.PlatformServices;

/// <summary>
/// Clipboard access through the WinRT <see cref="Clipboard"/> API, which Uno backs
/// with the native pasteboard on every head. Same call the share fallback uses.
/// </summary>
public class ClipboardService : IClipboardService
{
	public Task SetTextAsync(string text)
	{
		var package = new DataPackage();
		package.SetText(text ?? string.Empty);
		Clipboard.SetContent(package);
		return Task.CompletedTask;
	}
}
