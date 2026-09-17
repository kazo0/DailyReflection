using DailyReflection.Services.Share;
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
/// <para>
/// MVUX runs model command methods on a thread-pool thread (<c>Task.Run</c>),
/// so <see cref="ShareText"/> is usually called off the UI thread. The native
/// share sheets (UIActivityViewController, the Android chooser) and the
/// fallback's clipboard + ContentDialog all need the UI thread, and a failure
/// there is only logged by Uno — the share button silently did nothing. Every
/// call is therefore marshalled onto the main window's dispatcher first.
/// </para>
/// </summary>
public class ShareService : IShareService
{
	public Task ShareText(string title, string body)
	{
		var dispatcher = (Application.Current as App)?.MainWindow?.DispatcherQueue;
		if (dispatcher is null || dispatcher.HasThreadAccess)
		{
			return ShareOnUIThread(title, body);
		}

		var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var enqueued = dispatcher.TryEnqueue(async () =>
		{
			try
			{
				await ShareOnUIThread(title, body);
				completion.SetResult();
			}
			catch (Exception ex)
			{
				completion.SetException(ex);
			}
		});

		if (!enqueued)
		{
			completion.SetException(new InvalidOperationException("Could not dispatch the share request to the UI thread."));
		}

		return completion.Task;
	}

	private static Task ShareOnUIThread(string title, string body)
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
