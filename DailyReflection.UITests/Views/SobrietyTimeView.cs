using DailyReflection.Constants;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.UITest;
using Query = System.Func<Xamarin.UITest.Queries.AppQuery, Xamarin.UITest.Queries.AppQuery>;

namespace DailyReflection.UITests.Views
{
	public class SobrietyTimeView : ViewBase
	{
		protected override string PageId => AutomationConstants.Sobriety_Time;

		private readonly Query _oneDayAtATime;

		public SobrietyTimeView(IApp app) : base(app)
		{
			_oneDayAtATime = x => x.Marked(AutomationConstants.ST_One_Day_At_A_Time);
		}

		public override void WaitForViewToLoad()
		{
			App.Tap(x => x.Marked(AutomationConstants.Shell_Tab_SoberTime));

			base.WaitForViewToLoad();
		}

		public string GetEncouragementText() => App.Query(_oneDayAtATime).FirstOrDefault()?.Text;
	}
}
