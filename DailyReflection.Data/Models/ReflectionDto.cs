using SQLite;

namespace DailyReflection.Data.Models;

/// <summary>
/// Row DTO for the embedded reflections database. sqlite-net needs a mutable
/// class with settable properties and a parameterless constructor, so this type
/// stays a POCO; the Services layer maps it to the immutable Reflection record
/// that the presentation layer consumes.
/// </summary>
[Table("DailyReflections")]
public class ReflectionDto
{
	[PrimaryKey, AutoIncrement]
	public int Id { get; set; }
	public int Month { get; set; }
	public int Day { get; set; }
	public string Title { get; set; }
	public string Reading { get; set; }
	public string Source { get; set; }
	public string Thought { get; set; }

	/// <summary>
	/// True for readings from the secular book, false for the A.A.
	/// <i>Daily Reflections</i> text. The two sets share a date, so the DB's
	/// DateLookup index is UNIQUE(Month, Day, IsSecular).
	/// </summary>
	public bool IsSecular { get; set; }
}
