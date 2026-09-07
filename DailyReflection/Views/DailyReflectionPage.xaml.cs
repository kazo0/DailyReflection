using DailyReflection.Presentation.Models;

namespace DailyReflection.Views;

/// <summary>
/// Page displaying the daily reflection content. The MVUX navigator assigns
/// the DataContext (generated <see cref="DailyReflectionViewModel"/>) and
/// XAML does the rest: the reflection feed drives the FeedView, and the
/// calendar flyout binds its date TwoWay through
/// <see cref="Converters.DateTimeToDateTimeOffsetConverter"/>. The page
/// deliberately holds no code-behind beyond InitializeComponent.
/// </summary>
public sealed partial class DailyReflectionPage : Page
{
	public DailyReflectionPage()
	{
		this.InitializeComponent();
	}
}
