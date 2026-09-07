using DailyReflection.Core.Constants;
using NUnit.Framework;
using System.IO;
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
	public void DailyReflectionPage_binds_the_date_picker_instead_of_handling_DatePicked()
	{
		// Closes ANALYSIS.md gap 10.1.7 — the picked date round-trips through a
		// TwoWay binding (the Xamarin original's pure-XAML pattern) rather than a
		// code-behind DatePicked handler mutating the model.
		var xaml = File.ReadAllText(Path.Combine(ViewsDir, "DailyReflectionPage.xaml"));
		Assert.That(xaml, Does.Match(@"Date=""\{Binding Date,\s*Mode=TwoWay,\s*Converter=\{StaticResource DateTimeToDateTimeOffsetConverter\}\}"""),
			"The calendar flyout must bind its DateTimeOffset to the model's Date state TwoWay.");
		Assert.That(xaml, Does.Not.Contain("DatePicked="),
			"DailyReflectionPage must not wire a DatePicked handler — the date is bound.");

		var codeBehind = File.ReadAllText(Path.Combine(ViewsDir, "DailyReflectionPage.xaml.cs"));
		Assert.That(codeBehind, Does.Not.Contain("void "),
			"DailyReflectionPage.xaml.cs must hold no event handlers — the page is fully bound.");
	}

	[Test]
	public void SettingsPage_binds_NotificationsEnabled_two_way()
	{
		var xaml = File.ReadAllText(Path.Combine(ViewsDir, "SettingsPage.xaml"));
		Assert.That(xaml, Does.Match(@"IsOn=""\{Binding NotificationsEnabled,\s*Mode=TwoWay\}"""),
			"Settings ToggleSwitch must bind IsOn TwoWay so the toggle persists (writes the MVUX state).");
		Assert.That(xaml, Does.Match(@"Visibility=""\{Binding NotificationsSupported,\s*Converter=\{StaticResource BoolToVisibilityConverter\}\}"""),
			"The whole Daily Notifications section must be hidden when NotificationsSupported is false (spec 002).");
	}

	[Test]
	public void MainPage_shell_offers_both_tab_bars_over_one_content_region()
	{
		var xaml = File.ReadAllText(Path.Combine(ViewsDir, "MainPage.xaml"));

		// Two TabBars — bottom for narrow windows, vertical rail for wide ones — both
		// region-attached and both kept in the tree; the responsive markup only toggles
		// their visibility, so navigation never re-attaches.
		Assert.That(Regex.Matches(xaml, @"<utu:TabBar\b").Count, Is.EqualTo(2),
			"The shell is exactly two TabBars.");
		Assert.That(xaml, Does.Contain("Style=\"{StaticResource VerticalTabBarStyle}\""),
			"Wide windows navigate through a vertical TabBar rail.");
		Assert.That(xaml, Does.Contain("Style=\"{StaticResource BottomTabBarStyle}\""),
			"Narrow windows keep the bottom TabBar.");
		Assert.That(xaml, Does.Contain("utu:Responsive"),
			"The shell switches on the Toolkit's Responsive markup extension.");
		Assert.That(xaml, Does.Not.Contain("<NavigationView"),
			"The shell is TabBar-only — no NavigationView (its Material/Fluent styling and settings-item wiring are not worth it here).");

		// One shared content region, declared empty — the navigator injects the views.
		Assert.That(Regex.Matches(xaml, @"uen:Region\.Navigator=""Visibility""").Count, Is.EqualTo(1),
			"Exactly one content region is shared by both TabBars.");

		// Every route needs an entry in both bars, keyed by the same region name.
		foreach (var region in new[] { "Reflection", "SoberTime", "Settings" })
		{
			Assert.That(Regex.Matches(xaml, $@"uen:Region\.Name=""{region}""").Count, Is.EqualTo(2),
				$"Route '{region}' must appear once in each TabBar.");
		}

		var codeBehind = File.ReadAllText(Path.Combine(ViewsDir, "MainPage.xaml.cs"));
		Assert.That(codeBehind, Does.Not.Contain("void "),
			"MainPage.xaml.cs must hold no logic — the shell is fully declarative.");
	}

	[Test]
	public void SettingsPage_version_card_copies_through_a_command_and_shows_a_toast()
	{
		var xaml = File.ReadAllText(Path.Combine(ViewsDir, "SettingsPage.xaml"));
		Assert.That(xaml, Does.Contain("utu:CommandExtensions.Command=\"{Binding CopyVersion}\""),
			"The version card invokes the generated CopyVersion command via the Toolkit command extension — no code-behind handler.");
		Assert.That(xaml, Does.Match(@"Visibility=""\{Binding VersionCopied,\s*Converter=\{StaticResource BoolToVisibilityConverter\}\}"""),
			"The copied toast shows while the VersionCopied state is true.");
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
		// The flat path only carries a feed while Reflection is a record (MVUX then
		// generates a Bindable<Reflection> proxy for the property). GeneratedViewModelTests
		// guards that half; this only pins the XAML.
		Assert.That(xaml, Does.Contain("Source=\"{Binding DailyReflection}\""),
			"FeedView.Source binds the generated view-model's feed property directly.");
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
