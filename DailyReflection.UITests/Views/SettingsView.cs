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
		private readonly Query _timePicker;
		private readonly Query _datePicker;
		private readonly Query _platformDatePicker;
		private readonly Query _platformTimePicker;
		private readonly Query _switch;

		public SettingsView(IApp app, Platform platform) : base(app, platform)
		{
			_timePicker = x => x.Marked(AutomationConstants.Settings_Time_Picker);
			_datePicker = x => x.Marked(AutomationConstants.Settings_Date_Picker); 
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

		public bool IsTimePickerEnabled() => App.WaitForElement(_timePicker).FirstOrDefault().Enabled;
		public bool IsTimePickerOpen() => App.WaitForElement(_platformTimePicker).FirstOrDefault() != null;
		public bool IsDatePickerOpen() => App.WaitForElement(_platformDatePicker).FirstOrDefault() != null;
		public void EnableNotifications() => App.Tap(_switch);
		public void OpenTimePicker() => App.Tap(_timePicker);
		public void OpenDatePicker() => App.Tap(_datePicker);
	}
}
