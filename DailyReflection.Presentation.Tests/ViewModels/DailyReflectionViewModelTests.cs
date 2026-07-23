using DailyReflection.Data.Models;
using DailyReflection.Presentation.ViewModels;
using DailyReflection.Services.DailyReflection;
using DailyReflection.Services.Share;
using Moq;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace DailyReflection.Presentation.Tests.ViewModels
{
	public class DailyReflectionViewModelTests : ViewModelTestBase<DailyReflectionViewModel>
	{
		private Mock<IDailyReflectionService> _dailyReflectionService = null!;
		private Mock<IShareService> _shareService = null!;
		private Reflection _testReflection = null!;

		protected override DailyReflectionViewModel GetViewModel()
		{
			_dailyReflectionService = new Mock<IDailyReflectionService>();
			_shareService = new Mock<IShareService>();
			_testReflection = new Reflection
			{
				Id = 123,
				Reading = "Test Reading",
				Source = "Test Source",
				Thought = "Test Thought",
				Title = "Test",
			};

			_dailyReflectionService.Setup(x => x.GetDailyReflection(It.IsAny<DateTime?>()))
				.ReturnsAsync(_testReflection);

			return new DailyReflectionViewModel(_dailyReflectionService.Object, _shareService.Object);
		}

		public override async Task Setup()
		{
			await base.Setup();
			await ViewModelUnderTest.Init();
		}

		[Test]
		public async Task GetReflectionCommand_Calls_Daily_Reflection_Service()
		{
			await ViewModelUnderTest.GetDailyReflectionCommand.ExecuteAsync(null);
			_dailyReflectionService.Verify(x => x.GetDailyReflection(DateTime.Today), Times.Exactly(2));
			Assert.That(ViewModelUnderTest.DailyReflection.Id, Is.EqualTo(_testReflection.Id));
		}

		[Test]
		public void Share_Calls_Share_Service()
		{
			ViewModelUnderTest.ShareCommand.Execute(null);

			_shareService.Verify(x => x.ShareText(
					$"Daily Reflection {DateTime.Today:MMM d}",
					_testReflection.ToString()),
				Times.Once);
		}

		[Test]
		public async Task Null_Reflection_Sets_Error()
		{
			_dailyReflectionService.Reset();
			_dailyReflectionService.Setup(x => x.GetDailyReflection(It.IsAny<DateTime?>()))
				.ReturnsAsync(default(Reflection));

			await ViewModelUnderTest.GetDailyReflectionCommand.ExecuteAsync(null);

			_dailyReflectionService.Verify(x => x.GetDailyReflection(DateTime.Today), Times.Once);

			Assert.That(ViewModelUnderTest.HasError, Is.True);
		}

		[Test]
		public async Task GetReflectionCommand_Sets_Date()
		{
			await ViewModelUnderTest.GetDailyReflectionCommand.ExecuteAsync(new DateTime(2020, 12, 31));

			Assert.That(ViewModelUnderTest.Date, Is.EqualTo(new DateTime(2020, 12, 31)));
		}

		[Test]
		public async Task Init_Is_Idempotent()
		{
			await ViewModelUnderTest.Init();
			await ViewModelUnderTest.Init();

			// Setup() already invoked Init() once; subsequent calls must not re-fetch.
			_dailyReflectionService.Verify(x => x.GetDailyReflection(It.IsAny<DateTime?>()), Times.Once);
		}
	}
}
