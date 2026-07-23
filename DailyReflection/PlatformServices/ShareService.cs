using DailyReflection.Services.Share;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;

namespace DailyReflection.PlatformServices;

/// <summary>
/// Share service implementation using <see cref="DataTransferManager"/>.
/// <para>
/// Spec 007 fixes the original implementation's three issues:
/// </para>
/// <list type="bullet">
///   <item>Per-call closure captures title / body so concurrent <c>ShareText</c>
///         calls don't clobber each other's payload.</item>
///   <item>The <c>DataRequested</c> handler unsubscribes itself the moment it
///         has supplied the data; no leaked subscription survives the call.</item>
///   <item>On platforms where <c>DataTransferManager.IsSupported</c> is false
///         (notably Linux X11) the body is copied to the clipboard and a
///         "not supported" dialog is shown rather than silently failing.</item>
/// </list>
/// </summary>
public class ShareService : IShareService
{
	public Task ShareText(string title, string body)
	{
		if (!DataTransferManager.IsSupported())
		{
			return ShareUnsupportedFallback(title, body);
		}

		var dtm = DataTransferManager.GetForCurrentView();

		void Handler(DataTransferManager sender, DataRequestedEventArgs args)
		{
			try
			{
				args.Request.Data.Properties.Title = title ?? "Share";
				args.Request.Data.SetText(body ?? string.Empty);
			}
			finally
			{
				sender.DataRequested -= Handler;
			}
		}

		dtm.DataRequested += Handler;
		try
		{
			DataTransferManager.ShowShareUI();
		}
		catch
		{
			// If ShowShareUI throws synchronously, free the handler so we don't leak.
			dtm.DataRequested -= Handler;
			throw;
		}

		return Task.CompletedTask;
	}

	private static async Task ShareUnsupportedFallback(string title, string body)
	{
		// Best-effort clipboard copy so the user can paste the reflection
		// somewhere; matches Xamarin.Essentials.IShare's "throws on unsupported"
		// behaviour with friendly UI on top.
		try
		{
			var package = new DataPackage();
			package.SetText(string.IsNullOrEmpty(body) ? string.Empty : body);
			Clipboard.SetContent(package);
		}
		catch
		{
			// Some Linux desktops have no clipboard server running. Swallow
			// — the dialog below still informs the user.
		}

		var window = (Application.Current as App)?.MainWindow;
		var xamlRoot = window?.Content?.XamlRoot;
		if (xamlRoot is null)
		{
			return;
		}

		var dialog = new ContentDialog
		{
			Title = string.IsNullOrEmpty(title) ? "Sharing not available" : title,
			Content = "Sharing isn't available on this platform — the reflection has been copied to your clipboard.",
			CloseButtonText = "OK",
			XamlRoot = xamlRoot,
		};

		await dialog.ShowAsync();
	}
}
