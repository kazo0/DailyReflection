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
	Task<Models.ReflectionDto> GetReflection(DateTime date, bool secular = false);
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

		var assembly = typeof(DailyReflectionDatabase).GetTypeInfo().Assembly;
		var manifestName = assembly.GetManifestResourceNames()
			.FirstOrDefault(n => n.EndsWith(fileName, StringComparison.OrdinalIgnoreCase));

		if (manifestName == null)
		{
			throw new InvalidOperationException($"Failed to find resource [{fileName}]");
		}

		using Stream embeddedStream = assembly.GetManifestResourceStream(manifestName);

		// The extracted copy is a cache of the embedded resource, so a size mismatch
		// means the app shipped a new database (new readings, or — as when the secular
		// set was added — new columns) and the stale copy has to be replaced. Relying
		// on the version-gated RefreshDatabaseFile alone is not enough: its thresholds
		// only fire for upgrades from below them, so an install that has already
		// extracted an older file would keep querying a schema that no longer matches.
		if (File.Exists(path) && new FileInfo(path).Length == embeddedStream.Length)
		{
			return path;
		}

		using var fileStream = File.Create(path);
		embeddedStream.Seek(0, SeekOrigin.Begin);
		embeddedStream.CopyTo(fileStream);

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

	/// <summary>
	/// The reading for <paramref name="date"/>. The secular set is incomplete
	/// (263 of 366 days), so a missing secular entry falls back to the A.A. one
	/// rather than leaving the day blank.
	/// </summary>
	public async Task<Models.ReflectionDto> GetReflection(DateTime date, bool secular = false)
	{
		if (secular && await Find(date, isSecular: true) is { } secularReflection)
		{
			return secularReflection;
		}

		return await Find(date, isSecular: false);
	}

	private Task<Models.ReflectionDto> Find(DateTime date, bool isSecular)
		=> _db.Table<Models.ReflectionDto>()
			.FirstOrDefaultAsync(d => d.Day == date.Day && d.Month == date.Month && d.IsSecular == isSecular);
}
