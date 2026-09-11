using DailyReflection.Core.Extensions;

namespace DailyReflection.Core.Entities;

/// <summary>
/// A day's reflection as the rest of the app talks about it: an immutable
/// record mapped from the sqlite row DTO (<c>Data.Models.ReflectionDto</c>) by
/// <c>Services.DailyReflection.ReflectionMapping</c>.
/// <para>
/// Records are what MVUX wants for feed values — it generates a bindable proxy
/// for them, so a view can bind <c>FeedView.Source</c> straight at the feed
/// property. A mutable POCO would collapse to its unwrapped value on the
/// generated view-model instead, and the FeedView would render nothing.
/// </para>
/// <para>
/// It lives in Core rather than beside its service on purpose: the reactive
/// generator emits the proxy into the record's own namespace and writes the
/// record's type name unqualified, so a record under
/// <c>DailyReflection.Services.DailyReflection</c> makes the leading
/// <c>DailyReflection</c> bind to that namespace instead of the root and the
/// generated file fails to compile (CS0234).
/// </para>
/// </summary>
public partial record Reflection
{
	public bool IsSecular { get; init; }

	public int Id { get; init; }

	public int Month { get; init; }

	public int Day { get; init; }

	public string Title { get; init; } = string.Empty;

	public string Reading { get; init; } = string.Empty;

	public string Source { get; init; } = string.Empty;

	public string Thought { get; init; } = string.Empty;

	/// <summary>
	/// The share payload — the whole reading with its inline HTML stripped.
	/// </summary>
	public override string ToString()
		=> $"{Title}\n\n{Reading}\n— {Source}\n\n{Thought}".StripHtml();
}
