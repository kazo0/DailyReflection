using CommunityToolkit.Mvvm.Messaging.Messages;
using DailyReflection.Data.Models;

namespace DailyReflection.Presentation.Messages;

public class SoberTimeDisplayPreferenceChangedMessage : ValueChangedMessage<SoberTimeDisplayPreference>
{
	public SoberTimeDisplayPreferenceChangedMessage(SoberTimeDisplayPreference value) : base(value)
	{

	}
}
