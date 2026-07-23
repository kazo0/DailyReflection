using DailyReflection.Core.Constants;
using Microsoft.Extensions.Configuration;
using SQLite;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace DailyReflection.Data.Databases;

public interface IDailyReflectionDatabase
{
	Task<Models.Reflection> GetReflection(DateTime date);
	Task RefreshDatabaseFile();
}

public class DailyReflectionDatabase : IDailyReflectionDatabase
{
	private SQLiteAsyncConnection _db;
	private readonly IConfiguration _config;

	public DailyReflectionDatabase(IConfiguration config)
	{
		_config = config;

		string path = CreateDatabaseFile();
		_db = new SQLiteAsyncConnection(path, SQLiteOpenFlags.ReadOnly);
	}

	private string CreateDatabaseFile()
	{
		var fileName = _config[ConfigurationConstants.DatabaseFileName];
		var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), fileName);

		if (!File.Exists(path))
		{
			var assembly = typeof(DailyReflectionDatabase).GetTypeInfo().Assembly;
			var manifestName = assembly.GetManifestResourceNames()
				.FirstOrDefault(n => n.EndsWith(fileName, StringComparison.OrdinalIgnoreCase));

			if (manifestName == null)
			{
				throw new InvalidOperationException($"Failed to find resource [{fileName}]");
			}

			Stream embeddedStream = assembly.GetManifestResourceStream(manifestName);

			using var fileStream = File.Create(path);
			embeddedStream.Seek(0, SeekOrigin.Begin);
			embeddedStream.CopyTo(fileStream);
		}

		return path;
	}

	public async Task RefreshDatabaseFile()
	{
		await _db.CloseAsync();

		var fileName = _config[ConfigurationConstants.DatabaseFileName];
		var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), fileName);

		if (File.Exists(path))
		{
			File.Delete(path);
		}

		_db = new SQLiteAsyncConnection(CreateDatabaseFile(), SQLiteOpenFlags.ReadOnly);
	}

	public Task<Models.Reflection> GetReflection(DateTime date)
		=> _db.Table<Models.Reflection>().FirstOrDefaultAsync(d => d.Day == date.Day && d.Month == date.Month);
}
