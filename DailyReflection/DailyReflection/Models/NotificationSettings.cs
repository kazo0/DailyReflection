using System;
using System.Collections.Generic;
using System.Text;

namespace DailyReflection.Models
{
	public class NotificationSettings
	{
		public TimeSpan Time { get; set; }
		public bool Vibrate { get; set; }
		public bool Sound { get; set; }
	}
}
