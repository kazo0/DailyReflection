using DailyReflection.Services.Review;
using System.Globalization;

namespace DailyReflection.PlatformServices;

/// <summary>
/// LocalSettings-backed store for the in-app review prompt's state (Spec 012 §B), kept
/// alongside <see cref="VersionTrackingService"/>'s <c>DR_VT_*</c> keys. Dates are stored as
/// invariant <c>yyyy-MM-dd</c> strings; a missing or unreadable value reads as "never".
/// </summary>
public class ReviewPromptStateStore : IReviewPromptStateStore
{
	private const string KeyFirstUse = "DR_RP_FirstUseDate";
	private const string KeyLastUse = "DR_RP_LastUseDate";
	private const string KeyUseDayCount = "DR_RP_UseDayCount";
	private const string KeyLastPrompt = "DR_RP_LastPromptDate";

	private const string DateFormat = "yyyy-MM-dd";

	private readonly ApplicationDataContainer _store = ApplicationData.Current.LocalSettings;

	public ReviewPromptState Load() => new(
		ReadDate(KeyFirstUse),
		ReadDate(KeyLastUse),
		_store.Values[KeyUseDayCount] is int count ? count : 0,
		ReadDate(KeyLastPrompt));

	public void Save(ReviewPromptState state)
	{
		WriteDate(KeyFirstUse, state.FirstUseDate);
		WriteDate(KeyLastUse, state.LastUseDate);
		_store.Values[KeyUseDayCount] = state.UseDayCount;
		WriteDate(KeyLastPrompt, state.LastPromptDate);
	}

	private DateOnly? ReadDate(string key)
		=> _store.Values[key] is string value
			&& DateOnly.TryParseExact(value, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
			? date
			: null;

	private void WriteDate(string key, DateOnly? date)
	{
		if (date is { } value)
		{
			_store.Values[key] = value.ToString(DateFormat, CultureInfo.InvariantCulture);
		}
		else
		{
			_store.Values.Remove(key);
		}
	}
}
