using Microsoft.Maui.Controls.Compatibility.Platform.Android;
using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.Controls.Platform.Compatibility;

namespace DailyReflection.Platforms.Android.Renderers
{
	public class CustomShellRenderer : ShellRenderer
	{
		protected override IShellToolbarAppearanceTracker CreateToolbarAppearanceTracker()
		{
			return new CustomToolbarAppearanceTracker();
		}


	}

	internal class CustomToolbarAppearanceTracker : IShellToolbarAppearanceTracker
	{
		public void Dispose()
		{
		}

		public void SetAppearance(AndroidX.AppCompat.Widget.Toolbar toolbar, IShellToolbarTracker toolbarTracker, ShellAppearance appearance)
		{
			toolbar.OverflowIcon?.SetTint(appearance.ForegroundColor.ToAndroid());
		}

		public void ResetAppearance(AndroidX.AppCompat.Widget.Toolbar toolbar, IShellToolbarTracker toolbarTracker)
		{
		}
	}
}
