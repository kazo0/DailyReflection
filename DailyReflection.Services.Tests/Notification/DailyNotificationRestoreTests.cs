using DailyReflection.Services.Notification;
using NUnit.Framework;
using System;

namespace DailyReflection.Services.Tests.Notification;

[TestFixture]
public class DailyNotificationRestoreTests
{
	[Test]
	public void The_untouched_default_time_is_restored_as_midnight()
	{
		// A missing preference and a persisted DateTime.MinValue are both 0.
		Assert.That(DateTime.MinValue.ToBinary(), Is.Zero);

		var time = DailyNotificationRestore.GetRestoreTime(notificationsEnabled: true, storedNotificationTime: 0L);

		Assert.That(time?.TimeOfDay, Is.EqualTo(TimeSpan.Zero));
	}

	[TestCase(0, 0)]
	[TestCase(8, 30)]
	public void A_chosen_time_is_restored(int hour, int minute)
	{
		var stored = new DateTime(2026, 5, 3, hour, minute, 0, DateTimeKind.Local);

		var time = DailyNotificationRestore.GetRestoreTime(notificationsEnabled: true, stored.ToBinary());

		Assert.That(time?.TimeOfDay, Is.EqualTo(new TimeSpan(hour, minute, 0)));
	}

	[TestCase(0L)]
	[TestCase(637_000_000_000_000_000L)]
	public void Nothing_is_restored_while_notifications_are_off(long stored)
	{
		Assert.That(DailyNotificationRestore.GetRestoreTime(notificationsEnabled: false, stored), Is.Null);
	}
}
