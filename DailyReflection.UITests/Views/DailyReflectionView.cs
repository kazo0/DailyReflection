using DailyReflection.Core.Constants;
using System;
using System.Linq;
using Xamarin.UITest;
using Query = System.Func<Xamarin.UITest.Queries.AppQuery, Xamarin.UITest.Queries.AppQuery>;

namespace DailyReflection.UITests.Views
{
	public class DailyReflectionView : ViewBase
	{
		protected override string PageId => AutomationConstants.Daily_Reflection;

		private readonly Query _reflectionTitle;
		private readonly Query _reflectionQuote;
		private readonly Query _reflectionQuoteSource;
		private readonly Query _reflectionThought;
		private readonly Query _reflectionCopyright;
		private readonly Query _shareButton;
		private readonly Query _dateButton;
		private readonly Query _platformDatePicker;

		public DailyReflectionView(IApp app, Platform platform) : base(app, platform)
		{
			_reflectionTitle = x => x.Marked(AutomationConstants.DR_Reflection_Title);
			_reflectionQuote = x => x.Marked(AutomationConstants.DR_Reflection_Quote);
			_reflectionQuoteSource = x => x.Marked(AutomationConstants.DR_Reflection_QuoteSource);
			_reflectionThought = x => x.Marked(AutomationConstants.DR_Reflection_Thought);
			_reflectionCopyright = x => x.Marked(AutomationConstants.DR_Reflection_Copyright);
			_shareButton = x => x.Marked(AutomationConstants.DR_Share_Reflection);
			_dateButton = x => x.Marked(AutomationConstants.DR_Change_Date);

			if (platform == Platform.Android)
			{
				_platformDatePicker = x => x.Class("datePicker");

			}
			else if (platform == Platform.iOS)
			{
				_platformDatePicker = x => x.Class("UIDatePicker");
			}
		}

		public void WaitForReflectionContent() => App.WaitForElement(_reflectionTitle);
		public string GetReflectionTitle() => App.Query(_reflectionTitle).FirstOrDefault()?.Text;
		public string GetReflectionQuote() => App.Query(_reflectionQuote).FirstOrDefault()?.Text;
		public string GetReflectionQuoteSource() => App.Query(_reflectionQuoteSource).FirstOrDefault()?.Text;
		public string GetReflectionThought() => App.Query(_reflectionThought).FirstOrDefault()?.Text;
		public string GetReflectionCopyright() => App.Query(_reflectionCopyright).FirstOrDefault()?.Text;

		public void ShareReflection()
		{
			if (Platform == Platform.Android)
			{
				App.Tap("More options");
			}
			App.Tap(_shareButton);
		}
		public void OpenDatePicker() => App.Tap(_dateButton);

		public bool IsDatePickerOpen() => App.WaitForElement(_platformDatePicker).FirstOrDefault() != null;
	}
}
