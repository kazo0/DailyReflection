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
		public SettingsView(IApp app) : base(app)
		{
			_timePicker = x => x.Marked(AutomationConstants.Settings_Time_Picker);
			_datePicker = x => x.Marked(AutomationConstants.Settings_Date_Picker);
		}

		protected override string PageId => AutomationConstants.Settings;

		public override void WaitForViewToLoad()
		{
			App.Tap(x => x.Marked(AutomationConstants.Shell_Tab_Settings));

			base.WaitForViewToLoad();
		}

		public bool IsTimePickerEnabled() => App.WaitForElement(_timePicker).FirstOrDefault().Enabled;
		public bool IsTimePickerOpen() => App.WaitForElement(x => x.Class("timePicker")).FirstOrDefault() != null;
		public bool IsDatePickerOpen() => App.WaitForElement(x => x.Class("datePicker")).FirstOrDefault() != null;

		public void EnableNotifications() => App.Tap(c => c.Class("Switch"));

		public void OpenTimePicker() => App.Tap(_timePicker);
		public void OpenDatePicker() => App.Tap(_datePicker);
	}
}
