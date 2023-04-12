using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using static Xamarin.Essentials.Permissions;

namespace DailyReflection.Droid.Permissions
{
	public class NotificationPermission : BasePlatformPermission
	{
		public override (string androidPermission, bool isRuntime)[] RequiredPermissions => GetPermissionList().ToArray();

		private static List<(string androidPermission, bool isRuntime)> GetPermissionList()
		{
			var perms = new List<(string androidPermission, bool isRuntime)>();
			
			if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
			{
				perms.Add((Android.Manifest.Permission.PostNotifications, true));
			}

			return perms;
		}
	}
}