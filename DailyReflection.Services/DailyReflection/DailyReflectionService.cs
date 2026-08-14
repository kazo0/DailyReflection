using DailyReflection.Core.Entities;
using DailyReflection.Data.Databases;
using System;
using System.Threading.Tasks;

namespace DailyReflection.Services.DailyReflection;

public interface IDailyReflectionService
{
	/// <summary>
	/// The reflection for <paramref name="date"/> (today when omitted), or
	/// <c>null</c> when the database holds no entry for that day — the model
	/// surfaces that as the feed's None/error state.
	/// </summary>
	Task<Reflection?> GetDailyReflection(DateTime? date = null, bool secular = false);
}
public class DailyReflectionService : IDailyReflectionService
{
	private readonly IDailyReflectionDatabase _dailyReflectionDatabase;

	public DailyReflectionService(IDailyReflectionDatabase dailyReflectionDatabase)
	{
		_dailyReflectionDatabase = dailyReflectionDatabase;
	}

	public async Task<Reflection?> GetDailyReflection(DateTime? date = null, bool secular = false)
	{
		var row = await _dailyReflectionDatabase.GetReflection(date ?? DateTime.Today, secular);
		return row.ToEntity();
	}
}
