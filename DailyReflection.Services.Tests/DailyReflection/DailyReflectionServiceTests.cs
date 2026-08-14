using DailyReflection.Data.Databases;
using DailyReflection.Data.Models;
using DailyReflection.Services.DailyReflection;
using Moq;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace DailyReflection.Services.Tests.DailyReflection;

public class DailyReflectionServiceTests : ServiceTestBase<DailyReflectionService>
{
	private Mock<IDailyReflectionDatabase> _database = null!;
	private ReflectionDto _reflection = null!;

	protected override DailyReflectionService GetService()
	{
		_database = new Mock<IDailyReflectionDatabase>();
		_reflection = new ReflectionDto
		{
			Id = 123,
			Reading = "Test Reading",
			Source = "Test Source",
			Thought = "Test Thought",
			Title = "Test",
		};

		_database.Setup(x => x.GetReflection(It.IsAny<DateTime>(), It.IsAny<bool>()))
			.ReturnsAsync(_reflection);

		return new DailyReflectionService(_database.Object);
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task GetReflection_Calls_Database(bool secular)
	{
		_reflection.IsSecular = secular;
		var reflection = await ServiceUnderTest.GetDailyReflection(new DateTime(2020, 12, 31), secular);

		_database.Verify(x => x.GetReflection(new DateTime(2020, 12, 31), secular), Times.Once);

		Assert.That(reflection, Is.Not.Null);
		Assert.That(reflection!.Id, Is.EqualTo(_reflection.Id));
		// The row DTO is mapped to the Reflection record — every field carries over.
		Assert.That(reflection.Title, Is.EqualTo(_reflection.Title));
		Assert.That(reflection.Reading, Is.EqualTo(_reflection.Reading));
		Assert.That(reflection.Source, Is.EqualTo(_reflection.Source));
		Assert.That(reflection.IsSecular, Is.EqualTo(secular));
		Assert.That(reflection.Thought, Is.EqualTo(_reflection.Thought));
	}

	[Test]
	public async Task GetReflection_With_No_Row_Returns_Null()
	{
		_database.Reset();
		_database.Setup(x => x.GetReflection(It.IsAny<DateTime>(), false))
			.ReturnsAsync(default(ReflectionDto)!);

		var reflection = await ServiceUnderTest.GetDailyReflection(new DateTime(2020, 12, 31));

		// A missing day maps to null, which the model surfaces as the feed's None state.
		Assert.That(reflection, Is.Null);
	}

	[Test]
	public async Task GetReflection_With_Null_Date_Returns_Today()
	{
		var reflection = await ServiceUnderTest.GetDailyReflection();

		_database.Verify(x => x.GetReflection(DateTime.Today, false), Times.Once);

		Assert.That(reflection, Is.Not.Null);
		Assert.That(reflection!.Id, Is.EqualTo(_reflection.Id));
	}
}
