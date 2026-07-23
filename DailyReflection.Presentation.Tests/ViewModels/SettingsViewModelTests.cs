using DailyReflection.Core.Constants;
using DailyReflection.Presentation.ViewModels;
using DailyReflection.Services.Notification;
using DailyReflection.Services.Settings;
using Moq;
using NUnit.Framework;
using System;

namespace DailyReflection.Presentation.Tests.ViewModels
{
	public class SettingsViewModelTests : ViewModelTestBase<SettingsViewModel>
	{
		private Mock<INotificationService> _notificationService = null!;
		private Mock<ISettingsService> _settingsService = null!;

		private bool _notificationsEnabled = true;
		private DateTime _soberDate = new DateTime(2020, 12, 31);
		private DateTime _notifTime = new DateTime(2020, 12, 31, 8, 30, 0);


		protected override SettingsViewModel GetViewModel()
		{
			_notificationService = new Mock<INotificationService>();
			_notificationService.SetupGet(x => x.IsSupported).Returns(true);
			_settingsService = new Mock<ISettingsService>();

			_settingsService.Setup(x => x.Get(PreferenceConstants.NotificationsEnabled, It.IsAny<bool>()))
				.Returns(_notificationsEnabled);
			_settingsService.Setup(x => x.Get(PreferenceConstants.NotificationTime, It.IsAny<DateTime>()))
				.Returns(_notifTime);
			_settingsService.Setup(x => x.Get(PreferenceConstants.SoberDate, It.IsAny<DateTime>()))
				.Returns(_soberDate);

			return new SettingsViewModel(_notificationService.Object, _settingsService.Object);
		}

		[Test]
		public void MaxDate_Is_Now()
		{
			Assert.That(ViewModelUnderTest.MaxDate, Is.EqualTo(DateTime.Today));
		}

		[Test]
		public void Settings_Retrieved_On_Load()
		{
			Assert.That(ViewModelUnderTest.NotificationsEnabled, Is.EqualTo(_notificationsEnabled));
			Assert.That(ViewModelUnderTest.NotificationTime, Is.EqualTo(_notifTime));
			Assert.That(ViewModelUnderTest.SoberDate, Is.EqualTo(_soberDate));

			_settingsService.Verify(x => x.Get(PreferenceConstants.NotificationsEnabled, It.IsAny<bool>()), Times.Once);
			_settingsService.Verify(x => x.Get(PreferenceConstants.NotificationTime, It.IsAny<DateTime>()), Times.Once);
			_settingsService.Verify(x => x.Get(PreferenceConstants.SoberDate, It.IsAny<DateTime>()), Times.Once);
		}

		[Test]
		public void Setting_NotificationsEnabled_False_Cancels_Notifications()
		{
			ViewModelUnderTest.NotificationsEnabled = false;

			_notificationService.Verify(x => x.CancelNotifications(), Times.Once);
		}

		[Test]
		public void Setting_NotificationsEnabled_False_Sets_Setting()
		{
			ViewModelUnderTest.NotificationsEnabled = false;

			_settingsService.Verify(x => x.Set(PreferenceConstants.NotificationsEnabled, false), Times.Once);
		}

		[Test]
		public void Setting_NotificationsEnabled_True_Schedules_Notification()
		{
			ViewModelUnderTest.NotificationsEnabled = false;
			ViewModelUnderTest.NotificationsEnabled = true;

			_notificationService.Verify(x => x.TryScheduleDailyNotification(_notifTime, true), Times.Once);
		}


		[Test]
		public void Setting_NotificationTime_With_Notifications_Enabled_Schedules_Notification()
		{
			ViewModelUnderTest.NotificationsEnabled = true;
			ViewModelUnderTest.NotificationTime = new DateTime(2020, 12, 31, 9, 0, 0);

			_notificationService.Verify(x => x.TryScheduleDailyNotification(new DateTime(2020, 12, 31, 9, 0, 0), true), Times.Once);
		}

		[Test]
		public void Setting_NotificationTime_Without_Notifications_Enabled_Does_Not_Schedule_Notification()
		{
			ViewModelUnderTest.NotificationsEnabled = false;
			ViewModelUnderTest.NotificationTime = new DateTime(2020, 12, 31, 9, 0, 0);

			_notificationService.Verify(x => x.TryScheduleDailyNotification(new DateTime(2020, 12, 31, 9, 0, 0), It.IsAny<bool>()), Times.Never);
		}

		[Test]
		public void Setting_NotificationTime_Sets_Setting()
		{
			ViewModelUnderTest.NotificationTime = new DateTime(2020, 12, 31, 9, 0, 0);

			_settingsService.Verify(x => x.Set(PreferenceConstants.NotificationTime, new DateTime(2020, 12, 31, 9, 0, 0)), Times.Once);
		}

		[Test]
		public void Setting_SoberDate_Sets_Setting()
		{
			ViewModelUnderTest.SoberDate = new DateTime(2020, 10, 20);

			_settingsService.Verify(x => x.Set(PreferenceConstants.SoberDate, new DateTime(2020, 10, 20)), Times.Once);
		}

		[Test]
		public void NotificationsSupported_Reflects_NotificationService()
		{
			Assert.That(ViewModelUnderTest.NotificationsSupported, Is.True);

			_notificationService.SetupGet(x => x.IsSupported).Returns(false);
			var vm = new SettingsViewModel(_notificationService.Object, _settingsService.Object);

			Assert.That(vm.NotificationsSupported, Is.False);
		}

		[Test]
		public void Setting_NotificationsEnabled_True_On_Unsupported_Platform_Reverts_To_False()
		{
			// Spec 002 §E — the toggle cannot be turned on when IsSupported = false.
			_notificationService.Reset();
			_notificationService.SetupGet(x => x.IsSupported).Returns(false);
			_settingsService.Setup(x => x.Get(PreferenceConstants.NotificationsEnabled, It.IsAny<bool>())).Returns(false);
			_settingsService.Setup(x => x.Get(PreferenceConstants.NotificationTime, It.IsAny<DateTime>())).Returns(_notifTime);
			_settingsService.Setup(x => x.Get(PreferenceConstants.SoberDate, It.IsAny<DateTime>())).Returns(_soberDate);

			var vm = new SettingsViewModel(_notificationService.Object, _settingsService.Object);

			vm.NotificationsEnabled = true;

			Assert.That(vm.NotificationsEnabled, Is.False, "NotificationsEnabled must revert to false when the platform is unsupported.");
			_settingsService.Verify(x => x.Set(PreferenceConstants.NotificationsEnabled, true), Times.Never);
			_notificationService.Verify(x => x.TryScheduleDailyNotification(It.IsAny<DateTime>(), It.IsAny<bool>()), Times.Never);
		}
	}
}
