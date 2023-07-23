using static Microsoft.Maui.ApplicationModel.Permissions;

namespace DailyReflection.Services.Notification
{
	public class NotificationPermission : BasePlatformPermission
	{
		public override (string androidPermission, bool isRuntime)[] RequiredPermissions => GetPermissionList().ToArray();

		private static List<(string androidPermission, bool isRuntime)> GetPermissionList()
		{
			var perms = new List<(string androidPermission, bool isRuntime)>();

			if (OperatingSystem.IsAndroidVersionAtLeast(33))
			{
				perms.Add((Android.Manifest.Permission.PostNotifications, true));
			}

			return perms;
		}
	}
}