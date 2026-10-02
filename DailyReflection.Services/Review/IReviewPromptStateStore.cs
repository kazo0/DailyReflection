namespace DailyReflection.Services.Review;

/// <summary>
/// Persists <see cref="ReviewPromptState"/> between launches. Implemented in the head against
/// <c>ApplicationData.LocalSettings</c> (Spec 012 §B).
/// </summary>
public interface IReviewPromptStateStore
{
	ReviewPromptState Load();

	void Save(ReviewPromptState state);
}
