using DailyReflection.Models;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
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
				var web = new HtmlWeb();
				var doc = new HtmlDocument();
				doc = await web.LoadFromWebAsync("https://www.aa.org/pages/en_US/daily-reflection");

				if (doc == null)
				{
					_logger.LogWarning("No DOM object found");
					return null;
				}

				return new Reflection
				{
					Title = doc.DocumentNode.Descendants().FirstOrDefault(n => n.HasClass("daily-reflection-header-title"))?.InnerHtml,
					Quote = doc.DocumentNode.Descendants().FirstOrDefault(n => n.HasClass("daily-reflection-header-content"))?.InnerHtml,
					QuoteSource = doc.DocumentNode.Descendants().FirstOrDefault(n => n.HasClass("daily-reflection-content-title"))?.InnerHtml,
					Thought = doc.DocumentNode.Descendants().FirstOrDefault(n => n.HasClass("daily-reflection-content"))?.InnerHtml,
					Copyright = doc.DocumentNode.Descendants().FirstOrDefault(n => n.HasClass("daily-reflection-copyright"))?.InnerHtml,
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
