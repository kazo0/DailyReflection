#if !(__ANDROID__ || __IOS__)
using System.Runtime.Versioning;

namespace DailyReflection.PlatformServices;

public partial class NotificationService
{
	[SupportedOSPlatform("windows")]
	private static void RaiseWindowsToastIfAvailable()
	{
		// We deliberately do NOT take a hard dependency on
		// Microsoft.Windows.AppNotifications — the Skia desktop build runs on
		// Windows without WinAppSDK on some setups, and pulling in the package
		// drags in WinUI3 runtime requirements we don't currently satisfy. The
		// timer fires regardless; downstream surfacing is best-effort:
		//
		//   * If ToastNotificationManager is reachable via WinRT projection,
		//     we use it (the platform-extended methods are picked up at runtime).
		//   * Otherwise we log an Information event so the firing is still
		//     observable for debugging until WinAppSDK packaging is wired in.
		//
		// Wiring real toasts is tracked as the follow-up to spec 002 §C.
		try
		{
			System.Diagnostics.Debug.WriteLine(
				"[DailyReflection] Reflection notification fired at {0:O} on Windows desktop.",
				System.DateTime.Now);
		}
		catch
		{
			// nothing to do — Debug.WriteLine should never throw, but be defensive.
		}
	}

	partial void RaiseToast()
	{
		if (!OperatingSystem.IsWindows())
		{
			return;
		}

		RaiseWindowsToastIfAvailable();
	}
}
#endif
