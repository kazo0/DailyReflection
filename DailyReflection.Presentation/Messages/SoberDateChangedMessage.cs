using Microsoft.Toolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.Text;

namespace DailyReflection.Presentation.Messages
{
	public sealed class SoberDateChangedMessage : ValueChangedMessage<DateTime>
	{
		public SoberDateChangedMessage(DateTime value) : base(value)
		{
		}
	}
}
