using DailyReflection.Core.Constants;
using NUnit.Framework;
using System.IO;
using System.Linq;
using System.Reflection;

namespace DailyReflection.Services.Tests;

/// <summary>
/// Spec 005 lint test — every <see cref="AutomationConstants"/> string must be
/// referenced (as a literal) by at least one of the Uno head's XAML files. UI
/// tests written against these stable selectors then keep working without per-
/// test path translation.
/// </summary>
[TestFixture]
public class AutomationConstantsCoverageTests
{
	private const string RelativeViewsDir = "DailyReflection/Views";

	[Test]
	public void Every_constant_appears_in_a_uno_view_xaml()
	{
		var viewsDir = ResolveViewsDir();
		Assert.That(viewsDir, Is.Not.Null,
			$"Could not locate '{RelativeViewsDir}' by walking up from the test directory.");
		Assert.That(Directory.Exists(viewsDir), Is.True);

		var allXaml = string.Join("\n",
			Directory.EnumerateFiles(viewsDir, "*.xaml", SearchOption.AllDirectories)
					 .Select(File.ReadAllText));

		var constants = typeof(AutomationConstants)
			.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
			.Where(f => f.IsLiteral && f.FieldType == typeof(string))
			.Select(f => (Name: f.Name, Value: (string)f.GetRawConstantValue()!))
			.ToList();

		Assert.That(constants, Is.Not.Empty);

		foreach (var (name, value) in constants)
		{
			Assert.That(allXaml, Does.Contain(value),
				$"AutomationConstants.{name} = \"{value}\" is not referenced in any Uno view XAML.");
		}
	}

	private static string? ResolveViewsDir()
	{
		// Walk up from the test directory until we find the repo root
		// (a directory that contains the Uno head). Robust to any depth of
		// bin/obj nesting and to running from the repo root.
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

		// Fallback: cwd-relative.
		var cwdProbe = Path.GetFullPath(RelativeViewsDir);
		return Directory.Exists(cwdProbe) ? cwdProbe : null;
	}
}
