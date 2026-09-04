using DailyReflection.Data.Databases;
using DailyReflection.Data.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DailyReflection.Services.DailyReflection;

public interface IDailyReflectionService
{
	Task<Reflection> GetDailyReflection(DateTime? date = null);

	/// <summary>Every reading in calendar order (Jan 1 → Dec 31, Feb 29 included).</summary>
	Task<List<Reflection>> GetAllReflections();
}
public class DailyReflectionService : IDailyReflectionService
{
	private readonly IDailyReflectionDatabase _dailyReflectionDatabase;

	public DailyReflectionService(IDailyReflectionDatabase dailyReflectionDatabase)
	{
		_dailyReflectionDatabase = dailyReflectionDatabase;
	}

	public async Task<Reflection> GetDailyReflection(DateTime? date = null)
	{
		return await _dailyReflectionDatabase.GetReflection(date ?? DateTime.Today);
	}

	public Task<List<Reflection>> GetAllReflections()
		=> _dailyReflectionDatabase.GetAllReflections();
}
