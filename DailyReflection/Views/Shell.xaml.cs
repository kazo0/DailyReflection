using Uno.Extensions.Hosting;

namespace DailyReflection.Views;

/// <summary>
/// Navigation root (see Shell.xaml). <see cref="IContentControlProvider"/> hands the
/// navigation host the ContentControl to use as its root region.
/// </summary>
public sealed partial class Shell : UserControl, IContentControlProvider
{
	public Shell()
	{
		this.InitializeComponent();
	}

	public ContentControl ContentControl => NavigationRoot;
}
