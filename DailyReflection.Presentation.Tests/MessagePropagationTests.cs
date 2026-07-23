using CommunityToolkit.Mvvm.Messaging;
using DailyReflection.Core.Constants;
using DailyReflection.Data.Models;
using DailyReflection.Presentation.Messages;
using DailyReflection.Presentation.ViewModels;
using DailyReflection.Services.Notification;
using DailyReflection.Services.Settings;
using Moq;
using NUnit.Framework;
using System;

namespace DailyReflection.Presentation.Tests;

/// <summary>
/// Spec 006 §F — when SettingsViewModel writes a new SoberDate it raises
/// <see cref="SoberDateChangedMessage"/>; an active SobrietyTimeViewModel
/// must reload the date and recompute the period without a full rebuild.
/// </summary>
[TestFixture]
public class MessagePropagationTests
{
	private Mock<ISettingsService> _settings = null!;
	private Mock<INotificationService> _notifications = null!;

	[SetUp]
	public void Setup()
	{
		// Each test starts with a clean WeakReferenceMessenger to keep
		// recipient lists from leaking between fixtures.
		WeakReferenceMessenger.Default.Reset();

		_settings = new Mock<ISettingsService>();
		_notifications = new Mock<INotificationService>();
		_notifications.SetupGet(n => n.IsSupported).Returns(true);

		_settings.Setup(s => s.Get(PreferenceConstants.NotificationsEnabled, It.IsAny<bool>())).Returns(false);
		_settings.Setup(s => s.Get(PreferenceConstants.NotificationTime, It.IsAny<DateTime>())).Returns(DateTime.MinValue);
		_settings.Setup(s => s.Get(PreferenceConstants.SoberTimeDisplay, It.IsAny<int>())).Returns(0);
	}

	[Test]
	public void Setting_SoberDate_Updates_Active_SobrietyTime_VM()
	{
		var initial = new DateTime(2024, 1, 1);
		var updated = DateTime.Today.AddDays(-30);

		_settings.Setup(s => s.Get(PreferenceConstants.SoberDate, It.IsAny<DateTime>()))
			.Returns(initial);

		var sober = new SobrietyTimeViewModel(_settings.Object) { IsActive = true };
		var settings = new SettingsViewModel(_notifications.Object, _settings.Object);

		// SettingsViewModel.OnSoberDateChanged sends SoberDateChangedMessage.
		// SobrietyTimeViewModel.Receive reads the new value back from settings —
		// adjust the mock so the next Get returns the updated date.
		_settings.Setup(s => s.Get(PreferenceConstants.SoberDate, It.IsAny<DateTime>()))
			.Returns(updated);

		settings.SoberDate = updated;

		Assert.That(sober.SoberDate, Is.EqualTo(updated));
		Assert.That(sober.TotalDaysSober, Is.EqualTo(30));
	}

	[Test]
	public void Inactive_SobrietyTime_VM_Does_Not_Receive_Updates()
	{
		_settings.Setup(s => s.Get(PreferenceConstants.SoberDate, It.IsAny<DateTime>()))
			.Returns(new DateTime(2024, 1, 1));

		var sober = new SobrietyTimeViewModel(_settings.Object) { IsActive = false };
		var settings = new SettingsViewModel(_notifications.Object, _settings.Object);

		_settings.Setup(s => s.Get(PreferenceConstants.SoberDate, It.IsAny<DateTime>()))
			.Returns(DateTime.Today.AddDays(-7));

		settings.SoberDate = DateTime.Today.AddDays(-7);

		Assert.That(sober.SoberDate, Is.EqualTo(new DateTime(2024, 1, 1)),
			"An inactive view-model must not respond to messages — IsActive=false detaches the recipient.");
	}
}
