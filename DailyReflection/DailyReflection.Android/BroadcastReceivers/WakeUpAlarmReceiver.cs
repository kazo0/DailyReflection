using System;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using DailyReflection.Core.Constants;
using DailyReflection.Droid.Services;

namespace DailyReflection.Droid.BroadcastReceivers
{
	[BroadcastReceiver(Enabled = true, Exported = true)]
	[IntentFilter(new [] { Intent.ActionBootCompleted, AlarmManager.ActionScheduleExactAlarmPermissionStateChanged }, 
		Categories = new[] { "android.intent.category.DEFAULT" })]
	public class WakeUpAlarmReceiver : BroadcastReceiver
	{
		public override void OnReceive(Context context, Intent intent)
		{
			var prefs = context.GetSharedPreferences(PreferenceConstants.PreferenceSharedName, FileCreationMode.Private);
			if (prefs?.GetBoolean(PreferenceConstants.NotificationsEnabled, false) ?? false)
			{
				var timePref = prefs.GetLong(PreferenceConstants.NotificationTime, 0L);
				var time = DateTime.FromBinary(timePref);

				var notificationService = new NotificationService();

				Task.Run(() => notificationService.TryScheduleDailyNotification(time, shouldRequestPermission: false));
			}
		}
	}
}