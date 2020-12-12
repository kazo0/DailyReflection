using DailyReflection.Data.Models;
using Microsoft.Toolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.Text;

namespace DailyReflection.Presentation.Messages
{
	public class SoberTimeDisplayPreferenceChangedMessage : ValueChangedMessage<SoberTimeDisplayPreference>
	{
		public SoberTimeDisplayPreferenceChangedMessage(SoberTimeDisplayPreference value) : base(value)
		{

		}
	}
}
