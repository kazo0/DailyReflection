using DailyReflection.Data.Models;
using Microsoft.Toolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.Text;

namespace DailyReflection.Presentation.Messages
{
	public sealed class ReflectionPreferenceChangedMessage : ValueChangedMessage<ReflectionPreference>
	{
		public ReflectionPreferenceChangedMessage(ReflectionPreference value) : base(value)
		{
		}
	}
}
