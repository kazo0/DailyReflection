﻿using Android.App;
using Android.Content;
using DailyReflection.Constants;
using DailyReflection.Services.Notification;

namespace DailyReflection.BroadcastReceivers
{
	[BroadcastReceiver(Enabled = true, Exported = true)]
	[IntentFilter(new[] { Intent.ActionBootCompleted, AlarmManager.ActionScheduleExactAlarmPermissionStateChanged },
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