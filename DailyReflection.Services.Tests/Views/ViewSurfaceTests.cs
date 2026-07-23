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
		Assert.That(xaml, Does.Match(@"IsOn=""\{x:Bind ViewModel\.NotificationsEnabled,\s*Mode=TwoWay\}"""),
			"Settings ToggleSwitch must bind IsOn TwoWay so the toggle persists.");
		Assert.That(xaml, Does.Contain("ViewModel.NotificationsSupported"),
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
	public void DailyReflectionPage_ProgressRing_is_last_child_of_inner_grid()
	{
		// Spec 003 §C — z-order: ProgressRing must come after StackPanel and the
		// error grid in the same parent so it draws on top.
		var xaml = File.ReadAllText(Path.Combine(ViewsDir, "DailyReflectionPage.xaml"));
		var stackPanelIdx = xaml.IndexOf("<StackPanel ", System.StringComparison.Ordinal);
		var progressRingIdx = xaml.IndexOf("<ProgressRing ", System.StringComparison.Ordinal);

		Assert.That(stackPanelIdx, Is.GreaterThan(0));
		Assert.That(progressRingIdx, Is.GreaterThan(stackPanelIdx),
			"ProgressRing must appear after StackPanel in markup so it z-orders above content.");
	}
}
