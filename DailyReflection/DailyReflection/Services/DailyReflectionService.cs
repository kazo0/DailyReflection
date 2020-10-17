using AngleSharp;
using DailyReflection.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DailyReflection.Services
{
	public interface IDailyReflectionService
	{
		Task<Reflection> GetDailyReflection();
	}
	public class DailyReflectionService : IDailyReflectionService
	{
		private readonly ILogger<DailyReflectionService> _logger;
		private readonly Microsoft.Extensions.Configuration.IConfiguration _config;

		public DailyReflectionService(
			ILogger<DailyReflectionService> logger, 
			Microsoft.Extensions.Configuration.IConfiguration config)
		{
			_logger = logger;
			_config = config;
		}

		public async Task<Reflection> GetDailyReflection()
		{
			try
			{
				var config = Configuration.Default.WithDefaultLoader();
				var context = BrowsingContext.New(config);

				var doc = await context.OpenAsync("https://www.aa.org/pages/en_US/daily-reflection");

				if (doc == null)
				{
					_logger.LogWarning("No DOM object found");
					return null;
				}

				return new Reflection
				{
					Title = doc.QuerySelector(".daily-reflection-header-title")?.InnerHtml,
					Quote = doc.QuerySelector(".daily-reflection-header-content")?.InnerHtml,
					QuoteSource = doc.QuerySelector(".daily-reflection-content-title")?.InnerHtml,
					Thought = doc.QuerySelector(".daily-reflection-content")?.InnerHtml,
					Copyright = doc.QuerySelector(".daily-reflection-copyright")?.InnerHtml,
				};
			}
			catch (Exception e)
			{
				_logger.LogError(e, "An error occured while retrieving the daily reflection");
				return null;
			}
		}
	}
}
