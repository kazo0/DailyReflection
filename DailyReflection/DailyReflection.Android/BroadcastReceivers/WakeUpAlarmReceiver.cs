using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using DailyReflection.Services;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace DailyReflection.Droid.BroadcastReceivers
{
	[BroadcastReceiver(Enabled = true)]
	[IntentFilter(new [] { "android.intent.action.QUICKBOOT_POWERON", Intent.ActionBootCompleted, Intent.ActionLockedBootCompleted}, 
		Categories = new[] { "android.intent.category.DEFAULT" })]
	public class WakeUpAlarmReceiver : BroadcastReceiver
	{
		public override void OnReceive(Context context, Intent intent)
		{
			if (Preferences.Get("NotificationsEnabled", false))
			{
				DependencyService.Get<INotificationService>().ScheduleDailyNotification();
			}
		}
	}
}