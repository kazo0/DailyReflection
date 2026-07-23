using DailyReflection.Core.Extensions;
using SQLite;

namespace DailyReflection.Data.Models;

[Table("DailyReflections")]
public class Reflection
{
	[PrimaryKey, AutoIncrement]
	public int Id { get; set; }
	public int Month { get; set; }
	public int Day { get; set; }
	public string Title { get; set; }
	public string Reading { get; set; }
	public string Source { get; set; }
	public string Thought { get; set; }

	public override string ToString()
	{
		return $"{Title}\n\n{Reading}\n— {Source}\n\n{Thought}".StripHtml();
	}
}
