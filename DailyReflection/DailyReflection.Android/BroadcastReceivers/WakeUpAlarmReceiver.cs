using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Preferences;
using DailyReflection.Droid.Services;

namespace DailyReflection.Droid.BroadcastReceivers
{
	[BroadcastReceiver(Enabled = true)]
	[IntentFilter(new [] { Intent.ActionBootCompleted }, 
		Categories = new[] { "android.intent.category.DEFAULT" })]
	public class WakeUpAlarmReceiver : BroadcastReceiver
	{
		public override void OnReceive(Context context, Intent intent)
		{
			var prefs = PreferenceManager.GetDefaultSharedPreferences(context);
			
			if (prefs.GetBoolean("NotificationsEnabled", false))
			{
				var timePref = prefs.GetLong("NotificationTime", 0L);
				var time = DateTime.FromBinary(timePref);

				var notificationService = new NotificationService();

				notificationService.ScheduleDailyNotification(time);
			}
		}
	}
}