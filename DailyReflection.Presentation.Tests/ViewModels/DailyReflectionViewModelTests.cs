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
		private Mock<IDailyReflectionService> _dailyReflectionService;
		private Mock<IShareService> _shareService;
		private Reflection _testReflection;

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
			await ViewModelUnderTest.GetReflectionCommand.ExecuteAsync(null);
			_dailyReflectionService.Verify(x => x.GetDailyReflection(DateTime.Today), Times.Exactly(2));
			Assert.AreEqual(_testReflection.Id, ViewModelUnderTest.DailyReflection.Id);
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

			await ViewModelUnderTest.GetReflectionCommand.ExecuteAsync(null);

			_dailyReflectionService.Verify(x => x.GetDailyReflection(DateTime.Today), Times.Once);
			
			Assert.IsTrue(ViewModelUnderTest.HasError);
		}

		[Test]
		public async Task GetReflectionCommand_Sets_Date()
		{
			await ViewModelUnderTest.GetReflectionCommand.ExecuteAsync(new DateTime(2020, 12, 31));

			Assert.AreEqual(new DateTime(2020, 12, 31), ViewModelUnderTest.Date);
		}
	}
}
