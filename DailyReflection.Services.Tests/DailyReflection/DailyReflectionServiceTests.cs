using DailyReflection.Data.Databases;
using DailyReflection.Data.Models;
using DailyReflection.Services.DailyReflection;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DailyReflection.Services.Tests.DailyReflection
{
	public class DailyReflectionServiceTests : ServiceTestBase<DailyReflectionService>
	{
		private Mock<IDailyReflectionDatabase> _database;
		private Reflection _reflection;
		
		protected override DailyReflectionService GetService()
		{
			_database = new Mock<IDailyReflectionDatabase>();
			_reflection = new Reflection
			{
				Id = 123,
				Reading = "Test Reading",
				Source = "Test Source",
				Thought = "Test Thought",
				Title = "Test",
			};

			_database.Setup(x => x.GetReflection(It.IsAny<DateTime>()))
				.ReturnsAsync(_reflection);

			return new DailyReflectionService(_database.Object);
		}

		[Test]
		public async Task GetReflection_Calls_Database()
		{
			var reflection = await ServiceUnderTest.GetDailyReflection(new DateTime(2020, 12, 31));

			_database.Verify(x => x.GetReflection(new DateTime(2020, 12, 31)), Times.Once);

			Assert.NotNull(reflection);
			Assert.AreEqual(_reflection.Id, reflection.Id);
		}

		[Test]
		public async Task GetReflection_With_Null_Date_Returns_Today()
		{
			var reflection = await ServiceUnderTest.GetDailyReflection();

			_database.Verify(x => x.GetReflection(DateTime.Today), Times.Once);

			Assert.NotNull(reflection);
			Assert.AreEqual(_reflection.Id, reflection.Id);
		}
	}
}
