using DailyReflection.Core.Constants;
using DailyReflection.Data.Models;
using DailyReflection.Presentation.ViewModels;
using DailyReflection.Services.Settings;
using Moq;
using NodaTime;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace DailyReflection.Presentation.Tests.ViewModels
{
	public class SobrietyTimeViewModelTests : ViewModelTestBase<SobrietyTimeViewModel>
	{
		private Mock<ISettingsService> _settingsService;
		private DateTime _soberDate = new DateTime(2020, 12, 31);
		private SoberTimeDisplayPreference _soberTimeDisplay = SoberTimeDisplayPreference.DaysOnly;
		protected override SobrietyTimeViewModel GetViewModel()
		{
			_settingsService = new Mock<ISettingsService>();

			_settingsService.Setup(s => s.Get(PreferenceConstants.SoberDate, It.IsAny<DateTime>()))
				.Returns(_soberDate);

			_settingsService.Setup(s => s.Get(PreferenceConstants.SoberTimeDisplay, It.IsAny<int>()))
				.Returns((int)_soberTimeDisplay);

			return new SobrietyTimeViewModel(_settingsService.Object);
		}

		[Test]
		public void SoberPeriod_Set_On_Load()
		{
			var soberLocalDate = new LocalDate(_soberDate.Year, _soberDate.Month, _soberDate.Day);
			var soberPeriod = new LocalDate(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day) - soberLocalDate;

			Assert.AreEqual(soberPeriod, ViewModelUnderTest.SoberPeriod);
		}

		[Test]
		public void SoberDate_Set_On_Load()
		{
			Assert.AreEqual(_soberDate, ViewModelUnderTest.SoberDate);
		}

		[Test]
		public void Settings_Service_Called_On_SoberDateChangedMessage()
		{
			_settingsService.Reset();
			_settingsService.Setup(s => s.Get(PreferenceConstants.SoberDate, It.IsAny<DateTime>()))
				.Returns(_soberDate);

			ViewModelUnderTest.Receive(new Messages.SoberDateChangedMessage(DateTime.Today));
			_settingsService.Verify(x => x.Get(PreferenceConstants.SoberDate, It.IsAny<DateTime>()), Times.Once);
		}

		[Test]
		public void Settings_Service_Called_On_SoberTimeDisplayPreferenceChangedMessage()
		{
			_settingsService.Reset();
			_settingsService.Setup(s => s.Get(PreferenceConstants.SoberTimeDisplay, It.IsAny<int>()))
				.Returns(0);

			ViewModelUnderTest.Receive(new Messages.SoberTimeDisplayPreferenceChangedMessage(SoberTimeDisplayPreference.DaysMonthsYears));
			_settingsService.Verify(x => x.Get(PreferenceConstants.SoberTimeDisplay, It.IsAny<int>()), Times.Once);
		}
	}
}
