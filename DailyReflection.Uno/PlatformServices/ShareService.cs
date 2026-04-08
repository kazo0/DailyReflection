using DailyReflection.Services.Share;
using Windows.ApplicationModel.DataTransfer;

namespace DailyReflection.PlatformServices;

/// <summary>
/// Share service implementation using Uno Platform's DataTransferManager
/// Based on Uno Platform Docs: features/windows-applicationmodel-datatransfer.md
/// Maps from MAUI Share.Default.RequestAsync() to WinUI DataTransferManager
/// </summary>
public class ShareService : IShareService
{
    private string? _pendingTitle;
    private string? _pendingBody;

    public Task ShareText(string title, string body)
    {
        if (!DataTransferManager.IsSupported())
        {
            // Sharing not supported on this platform
            return Task.CompletedTask;
        }

        _pendingTitle = title;
        _pendingBody = body;

        var dataTransferManager = DataTransferManager.GetForCurrentView();
        dataTransferManager.DataRequested += DataRequested;

        try
        {
            DataTransferManager.ShowShareUI();
        }
        finally
        {
            dataTransferManager.DataRequested -= DataRequested;
        }

        return Task.CompletedTask;
    }

    private void DataRequested(DataTransferManager sender, DataRequestedEventArgs args)
    {
        args.Request.Data.Properties.Title = _pendingTitle ?? "Share";
        args.Request.Data.SetText(_pendingBody ?? string.Empty);
    }
}
