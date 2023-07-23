﻿using DailyReflection.Data.Databases;
using DailyReflection.Data.Models;

namespace DailyReflection.Services.DailyReflection
{
	public interface IDailyReflectionService
	{
		Task<Reflection> GetDailyReflection(DateTime? date = null);
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
	}
}
