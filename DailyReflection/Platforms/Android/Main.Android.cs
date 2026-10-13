using Android.Runtime;
using Uno.UI.Hosting;

namespace DailyReflection.Uno.Droid;

[global::Android.App.ApplicationAttribute(
	Label = "@string/ApplicationName",
	Icon = "@mipmap/icon",
	LargeHeap = true,
	HardwareAccelerated = true,
	Theme = "@style/Theme.App.Starting"
)]
public class Application : global::Uno.UI.Runtime.Android.NativeApplication
{
	static Application()
	{
		App.InitializeLogging();
	}

	public Application(IntPtr javaReference, JniHandleOwnership transfer)
		: base(javaReference, transfer)
	{
	}

	// Uno 7.0: Android builds its host here (the equivalent of the other heads'
	// Main), called once when the first ApplicationActivity starts.
	protected override UnoPlatformHost CreateHost() =>
		UnoPlatformHostBuilder.Create()
			.App(() => new App())
			.UseAndroid()
			.Build();

}

