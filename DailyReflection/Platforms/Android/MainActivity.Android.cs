using Android.App;
using Android.OS;
using Android.Views;
using AndroidX.Activity;
using Windows.UI.Core;

namespace DailyReflection.Uno.Droid;

[Activity(
	MainLauncher = true,
	ConfigurationChanges = global::Uno.UI.ActivityHelper.AllConfigChanges,
	WindowSoftInputMode = SoftInput.AdjustNothing | SoftInput.StateHidden
)]
public class MainActivity : Microsoft.UI.Xaml.ApplicationActivity
{
	protected override void OnCreate(Bundle? savedInstanceState)
	{
		global::AndroidX.Core.SplashScreen.SplashScreen.InstallSplashScreen(this);

		base.OnCreate(savedInstanceState);

		// Registered after Uno's own callback (added in base.OnCreate), so it runs first.
		if (Build.VERSION.SdkInt >= BuildVersionCodes.Baklava)
		{
			OnBackPressedDispatcher.AddCallback(this, new RootBackPressedCallback(this));
		}
	}

	/// <summary>
	/// On Android 16+ Uno ignores <see cref="BackRequestedEventArgs.Handled"/>: while anything
	/// subscribes to <see cref="SystemNavigationManager.BackRequested"/> the app consumes every
	/// Back press, and each page's Toolkit NavigationBar subscribes for as long as it is loaded,
	/// so Back could never leave the app. This still offers each press to the app through Uno,
	/// with a probe subscribed last to see whether a handler took it, and when none did sends
	/// the task to the background — what the system does for a root activity, and what Back
	/// does on earlier Android versions.
	/// </summary>
	private sealed class RootBackPressedCallback(Activity activity) : OnBackPressedCallback(true)
	{
		public override void HandleOnBackPressed()
		{
			var navigationManager = SystemNavigationManager.GetForCurrentView();

			// Stays null when Uno's internal listeners (an open flyout or popup) handle
			// the press, since BackRequested is then never raised.
			bool? handled = null;
			void Probe(object? sender, BackRequestedEventArgs e) => handled = e.Handled;

			navigationManager.BackRequested += Probe;
			Enabled = false;
			try
			{
				((ComponentActivity)activity).OnBackPressedDispatcher.OnBackPressed();
			}
			finally
			{
				Enabled = true;
				navigationManager.BackRequested -= Probe;
			}

			if (handled == false)
			{
				activity.MoveTaskToBack(true);
			}
		}
	}
}
