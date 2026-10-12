namespace DailyReflection.Core.Entities;

/// <summary>
/// The reading text size selected in Settings (spec 013). Stored as its
/// integer value, so <see cref="Normal"/> — the size the Xamarin app used —
/// must stay 0: a fresh install, and every existing one, reads back as it.
/// Declaration order is the order the Settings picker lists them in. There is
/// no size below Normal: every report since the Uno port asked for larger.
/// </summary>
public enum ReadingTextSize
{
	Normal = 0,
	Large,
	ExtraLarge,
	Huge,
}

public static class ReadingTextSizeExtensions
{
	/// <summary>
	/// The factor the Reflection tab multiplies its base font sizes (and its
	/// column width) by. Anything stored outside the enum falls back to 1.
	/// </summary>
	public static double ToScale(this ReadingTextSize size) => size switch
	{
		ReadingTextSize.Large => 1.2,
		ReadingTextSize.ExtraLarge => 1.4,
		ReadingTextSize.Huge => 1.65,
		_ => 1.0,
	};
}
