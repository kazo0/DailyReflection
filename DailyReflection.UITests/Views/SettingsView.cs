using DailyReflection.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.UITest;
using Query = System.Func<Xamarin.UITest.Queries.AppQuery, Xamarin.UITest.Queries.AppQuery>;


namespace DailyReflection.UITests.Views
{
	public class SettingsView : ViewBase
	{
		private readonly Query _notifTimeCell;
		private readonly Query _soberDateCell;
		private readonly Query _platformDatePicker;
		private readonly Query _platformTimePicker;
		private readonly Query _switch;
		private readonly Query _notifTimeTextView;

		public SettingsView(IApp app, Platform platform) : base(app, platform)
		{
			_notifTimeCell = x => x.Marked(AutomationConstants.Settings_Notification_Time);
			_notifTimeTextView = x => x.Marked(AutomationConstants.Settings_Notification_Time).Child().Child();
			_soberDateCell = x => x.Marked(AutomationConstants.Settings_Sober_Date); 
			_switch = x => x.Switch();

			if (platform == Platform.Android)
			{
				_platformDatePicker = x => x.Class("datePicker");
				_platformTimePicker = x => x.Class("timePicker");
				
			}
			else if (platform == Platform.iOS)
			{
				_platformDatePicker = x => x.Class("UIDatePicker");
				_platformTimePicker = x => x.Class("UIDatePicker");
			}
		}

		protected override string PageId => AutomationConstants.Settings;

		public override void WaitForViewToLoad()
		{
			App.Tap(x => x.Marked(AutomationConstants.Shell_Tab_Settings));

			base.WaitForViewToLoad();
		}

		public bool IsTimePickerEnabled() => App.WaitForElement(_notifTimeTextView).FirstOrDefault().Enabled;
		public bool IsTimePickerOpen() => App.WaitForElement(_platformTimePicker).FirstOrDefault() != null;
		public bool IsDatePickerOpen() => App.WaitForElement(_platformDatePicker).FirstOrDefault() != null;
		public void EnableNotifications() => App.Tap(_switch);
		public void OpenTimePicker() => App.Tap(_notifTimeCell);
		public void OpenDatePicker() => App.Tap(_soberDateCell);
	}
}
