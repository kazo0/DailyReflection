using DailyReflection.Core.Constants;
using NUnit.Framework;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace DailyReflection.Services.Tests.Views;

/// <summary>
/// Spec 011 §C lite — smoke checks against the Uno views' XAML source. This
/// doesn't drive a UI thread (Uno.UITest is heavy and out of scope today),
/// but it does verify each page declares the controls a real UI test would
/// target. Catches accidental deletion of automation hooks.
/// </summary>
[TestFixture]
public class ViewSurfaceTests
{
	private const string RelativeViewsDir = "DailyReflection/Views";

	private static string ViewsDir
	{
		get
		{
			var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
			while (dir is not null)
			{
				var probe = Path.Combine(dir.FullName, RelativeViewsDir);
				if (Directory.Exists(probe))
				{
					return probe;
				}
				dir = dir.Parent;
			}
			return RelativeViewsDir;
		}
	}

	[Test]
	public void DailyReflectionPage_exposes_title_quote_thought_share_change_date()
	{
		var xaml = File.ReadAllText(Path.Combine(ViewsDir, "DailyReflectionPage.xaml"));
		Assert.That(xaml, Does.Contain(AutomationConstants.DR_Reflection_Title));
		Assert.That(xaml, Does.Contain(AutomationConstants.DR_Reflection_Quote));
		Assert.That(xaml, Does.Contain(AutomationConstants.DR_Reflection_Thought));
		Assert.That(xaml, Does.Contain(AutomationConstants.DR_Share_Reflection));
		Assert.That(xaml, Does.Contain(AutomationConstants.DR_Change_Date));
	}

	[Test]
	public void SettingsPage_binds_NotificationsEnabled_two_way()
	{
		var xaml = File.ReadAllText(Path.Combine(ViewsDir, "SettingsPage.xaml"));
		Assert.That(xaml, Does.Match(@"IsOn=""\{Binding NotificationsEnabled,\s*Mode=TwoWay\}"""),
			"Settings ToggleSwitch must bind IsOn TwoWay so the toggle persists (writes the MVUX state).");
		Assert.That(xaml, Does.Contain("IsEnabled=\"{Binding NotificationsSupported}\""),
			"Settings ToggleSwitch must gate IsEnabled on NotificationsSupported (spec 002).");
	}

	[Test]
	public void SobrietyTimePage_uses_typed_DisplayPreference_parameter()
	{
		var xaml = File.ReadAllText(Path.Combine(ViewsDir, "SobrietyTimePage.xaml"));
		Assert.That(xaml, Does.Contain("DisplayPreference=\"DaysMonthsYears\""),
			"Spec 006 §D — converter parameter must be the typed enum value, not the legacy string indirection.");
		Assert.That(xaml, Does.Not.Match(@"DisplayPreferenceString=\"""),
			"Legacy DisplayPreferenceString=\"…\" attribute must not be used in XAML.");
	}

	[Test]
	public void MainPage_TabBar_uses_DRTabBarBackgroundBrush()
	{
		var xaml = File.ReadAllText(Path.Combine(ViewsDir, "MainPage.xaml"));
		Assert.That(xaml, Does.Contain("Background=\"{ThemeResource DRTabBarBackgroundBrush}\""));
	}

	[Test]
	public void Pages_use_Toolkit_NavigationBar_not_CommandBar()
	{
		foreach (var page in new[] { "DailyReflectionPage.xaml", "SobrietyTimePage.xaml", "SettingsPage.xaml" })
		{
			var xaml = File.ReadAllText(Path.Combine(ViewsDir, page));
			Assert.That(xaml, Does.Contain("<utu:NavigationBar"),
				$"{page} must use the Uno Toolkit NavigationBar as its top app bar.");
			Assert.That(xaml, Does.Not.Contain("<CommandBar"),
				$"{page} must not use a CommandBar as its top app bar.");
		}
	}

	[Test]
	public void DailyReflectionPage_uses_FeedView_with_progress_and_error_templates()
	{
		// Spec 003 §C — loading and error presentation is owned by the MVUX
		// FeedView now: it swaps templates instead of z-ordering a ProgressRing
		// above the content grid.
		var xaml = File.ReadAllText(Path.Combine(ViewsDir, "DailyReflectionPage.xaml"));
		Assert.That(xaml, Does.Contain("<mvux:FeedView"),
			"DailyReflectionPage must present the reflection through an MVUX FeedView.");
		Assert.That(xaml, Does.Contain("<mvux:FeedView.ProgressTemplate>"),
			"FeedView must declare a ProgressTemplate for the loading state.");
		Assert.That(xaml, Does.Match(@"<mvux:FeedView\.ProgressTemplate>\s*<DataTemplate>\s*<ProgressRing"),
			"ProgressTemplate must contain the ProgressRing.");
		Assert.That(xaml, Does.Contain("<mvux:FeedView.NoneTemplate>"),
			"FeedView must declare a NoneTemplate for the no-entry (error) state.");
		Assert.That(xaml, Does.Contain("<mvux:FeedView.ErrorTemplate>"),
			"FeedView must declare an ErrorTemplate for the failure state.");
	}
}
