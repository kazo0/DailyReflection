using CommunityToolkit.Mvvm.Messaging.Messages;

namespace DailyReflection.Presentation.Messages
{
	public sealed class SoberDateChangedMessage : ValueChangedMessage<DateTime>
	{
		public SoberDateChangedMessage(DateTime value) : base(value)
		{
		}
	}
}
