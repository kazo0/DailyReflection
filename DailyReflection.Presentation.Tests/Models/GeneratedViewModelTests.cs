using DailyReflection.Core.Entities;
using DailyReflection.Presentation.Models;
using NUnit.Framework;
using System.Reflection;
using Uno.Extensions.Reactive;

namespace DailyReflection.Presentation.Tests.Models;

/// <summary>
/// Guards the shape of the MVUX-generated view-models that the XAML binds to.
/// These are compile-time generated, so a generator or model change can silently
/// alter what a binding resolves to — the views fail by rendering nothing rather
/// than by throwing. The generated types can't be instantiated here (their base
/// ctor resolves an Uno dispatcher, which this net10.0 project only has as a ref
/// assembly), so these assert the generated shape by reflection.
/// </summary>
[TestFixture]
public class GeneratedViewModelTests
{
	[Test]
	public void DailyReflection_is_exposed_as_a_feed_so_FeedView_can_bind_it_directly()
	{
		var property = typeof(DailyReflectionViewModel).GetProperty(
			nameof(DailyReflectionModel.DailyReflection),
			BindingFlags.Public | BindingFlags.Instance);

		Assert.That(property, Is.Not.Null,
			"DailyReflectionPage binds FeedView.Source to this property.");
		Assert.That(typeof(IFeed<Reflection>).IsAssignableFrom(property!.PropertyType), Is.True,
			$"Expected a feed-carrying bindable proxy, got {property.PropertyType.Name}. MVUX only "
			+ "generates one while the feed's value type is a record — turn Reflection back into a "
			+ "class (or move it under a namespace the generator mis-resolves) and this collapses to "
			+ "the unwrapped value, which makes FeedView render blank with no error.");
	}

	[Test]
	public void States_stay_flattened_to_their_values_for_leaf_bindings()
	{
		// The other half of the contract: two-way-bound states surface as plain
		// values (Date, NotificationsEnabled...), which is what the XAML binds.
		Assert.That(typeof(DailyReflectionViewModel).GetProperty(nameof(DailyReflectionModel.Date))!.PropertyType,
			Is.EqualTo(typeof(System.DateTime)));
		Assert.That(typeof(SettingsViewModel).GetProperty(nameof(SettingsModel.NotificationsEnabled))!.PropertyType,
			Is.EqualTo(typeof(bool)));
	}
}
