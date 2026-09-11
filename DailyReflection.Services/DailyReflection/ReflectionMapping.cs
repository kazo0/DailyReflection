using DailyReflection.Core.Entities;
using DailyReflection.Data.Models;

namespace DailyReflection.Services.DailyReflection;

/// <summary>
/// DTO → entity mapping. The only place the sqlite row shape crosses into the
/// service layer; everything above <see cref="DailyReflectionService"/> sees
/// <see cref="Reflection"/> records.
/// </summary>
internal static class ReflectionMapping
{
	public static Reflection? ToEntity(this ReflectionDto? dto)
		=> dto is null
			? null
			: new Reflection
			{
				Id = dto.Id,
				IsSecular = dto.IsSecular,
				Month = dto.Month,
				Day = dto.Day,
				Title = dto.Title ?? string.Empty,
				Reading = dto.Reading ?? string.Empty,
				Source = dto.Source ?? string.Empty,
				Thought = dto.Thought ?? string.Empty,
			};
}
