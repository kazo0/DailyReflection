using Microsoft.Maui.Handlers;

public static class PickerExtensions
{
	public static void Open(this DatePicker picker)
	{
		OpenCore(picker);
	}

	public static void Open(this TimePicker picker)
	{
		OpenCore(picker);
	}

	public static void Open(this Picker picker)
	{
		OpenCore(picker);
	}

	private static void OpenCore(Microsoft.Maui.Controls.View picker) 
	{
			if (picker?.Handler is not {} handler)
			{
				return;
			}
#if IOS
			handler.VirtualView.IsFocused = false;
			picker.Focus();
#endif
#if ANDROID
			if (handler.PlatformView is Android.Views.View view)
			{
				view.PerformClick();
			}
#endif
	}
}