using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms.Internals;
using Xamarin.UITest;
using Query = System.Func<Xamarin.UITest.Queries.AppQuery, Xamarin.UITest.Queries.AppQuery>;

namespace DailyReflection.UITests.Views
{
	public abstract class ViewBase
	{
		protected IApp App { get; }
		protected Platform Platform { get; }
		protected abstract string PageId { get; }

		protected ViewBase(IApp app, Platform platform)
		{
			App = app;
			Platform = platform;
		}

		public virtual void WaitForViewToLoad() => App.WaitForElement(x => x.Marked(PageId));
	}
}
