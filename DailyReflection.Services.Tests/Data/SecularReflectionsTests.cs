using DailyReflection.Core.Constants;
using DailyReflection.Data.Databases;
using Microsoft.Extensions.Configuration;
using Moq;
using NUnit.Framework;
using System;
using System.IO;
using System.Threading.Tasks;

namespace DailyReflection.Services.Tests.Data;

/// <summary>
/// Exercises the real embedded database rather than a mock. Both sets cover
/// the whole year, so <see cref="DailyReflectionDatabase.GetReflection"/> should
/// never need its fall back to the A.A. reading — a secular day that resolves
/// to the A.A. text means a row went missing. These are the invariants the
/// Reflection tab depends on.
/// </summary>
[TestFixture(Category = "Service Tests")]
public class SecularReflectionsTests
{
	// The extract path resolves the embedded resource with EndsWith, so any
	// suffix of "dailyreflections.db" loads the real database while keeping the
	// extracted copy off the app's own file (which the test run would otherwise
	// lock, and which belongs to the installed app).
	private const string FileName = "yreflections.db";

	private DailyReflectionDatabase _db = null!;

	internal static DailyReflectionDatabase CreateDatabase(string fileName)
	{
		var config = new Mock<IConfiguration>();
		config.SetupGet(c => c[ConfigurationConstants.DatabaseFileName]).Returns(fileName);

		return new DailyReflectionDatabase(config.Object);
	}

	internal static string LocalPath(string fileName) => Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), fileName);

	[SetUp]
	public void Setup() => _db = CreateDatabase(FileName);

	[Test]
	public async Task Every_Day_Has_A_Traditional_Reading()
	{
		foreach (var date in AllDays())
		{
			var reflection = await _db.GetReflection(date);

			Assert.That(reflection, Is.Not.Null, $"no reading for {date:MMM d}");
			Assert.That(reflection.IsSecular, Is.False);
		}
	}

	[Test]
	public async Task Every_Day_Has_A_Secular_Reading()
	{
		var days = 0;

		foreach (var date in AllDays())
		{
			var reflection = await _db.GetReflection(date, secular: true);

			Assert.That(reflection, Is.Not.Null, $"no reading for {date:MMM d}");
			Assert.That(reflection.IsSecular, Is.True,
				$"{date:MMM d} fell back to the A.A. text — its secular row is missing");
			days++;
		}

		Assert.That(days, Is.EqualTo(366));
	}

	[Test]
	public async Task Secular_And_Traditional_Readings_Differ()
	{
		// Jan 1 is covered by both sets.
		var date = new DateTime(2024, 1, 1);

		var traditional = await _db.GetReflection(date);
		var secular = await _db.GetReflection(date, secular: true);

		Assert.That(secular.IsSecular, Is.True);
		Assert.That(secular.Reading, Is.Not.EqualTo(traditional.Reading));
		Assert.That(secular.Title, Is.EqualTo("January 1"));
	}

	[Test]
	public async Task Source_Is_Never_Markup()
	{
		foreach (var date in AllDays())
		{
			var secular = await _db.GetReflection(date, secular: true);

			// The view binds Source into a plain Run — markup would render literally.
			Assert.That(secular.Source, Does.Not.Contain("<"), $"{date:MMM d} has markup in Source");
		}
	}

	private static System.Collections.Generic.IEnumerable<DateTime> AllDays()
	{
		// 2024 is a leap year, so this covers Feb 29.
		for (var date = new DateTime(2024, 1, 1); date.Year == 2024; date = date.AddDays(1))
		{
			yield return date;
		}
	}
}

/// <summary>
/// Its own fixture (and its own extracted file) so no other test holds the
/// SQLite handle open while this one overwrites it.
/// </summary>
[TestFixture(Category = "Service Tests")]
public class StaleDatabaseFileTests
{
	private const string FileName = "lyreflections.db";

	[Test]
	public void Stale_Extracted_File_Is_Replaced()
	{
		var path = SecularReflectionsTests.LocalPath(FileName);

		// An install carrying a pre-secular database must not keep querying it:
		// the schema no longer matches, so every read would fail.
		File.WriteAllText(path, "stale");

		SecularReflectionsTests.CreateDatabase(FileName);

		Assert.That(new FileInfo(path).Length, Is.GreaterThan("stale".Length));
	}
}
