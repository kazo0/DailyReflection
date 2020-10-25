using DailyReflection.Constants;
using System;
using System.Linq;
using Xamarin.UITest;
using Query = System.Func<Xamarin.UITest.Queries.AppQuery, Xamarin.UITest.Queries.AppQuery>;

namespace DailyReflection.UITests.Views
{
	public class DailyReflectionView : ViewBase
	{
		protected override string PageTitle => DateTime.Now.ToString("MMMM dd");

		private readonly Query _reflectionTitle;
		private readonly Query _reflectionQuote;
		private readonly Query _reflectionQuoteSource;
		private readonly Query _reflectionThought;
		private readonly Query _reflectionCopyright;

		public DailyReflectionView(IApp app) : base(app)
		{
			_reflectionTitle = x => x.Marked(AutomationConstants.DR_Reflection_Title);
			_reflectionQuote = x => x.Marked(AutomationConstants.DR_Reflection_Quote);
			_reflectionQuoteSource = x => x.Marked(AutomationConstants.DR_Reflection_QuoteSource);
			_reflectionThought = x => x.Marked(AutomationConstants.DR_Reflection_Thought);
			_reflectionCopyright = x => x.Marked(AutomationConstants.DR_Reflection_Copyright);
		}

		public void WaitForReflectionContent() => App.WaitForElement(_reflectionTitle);
		public string GetReflectionTitle() => App.Query(_reflectionTitle).FirstOrDefault()?.Text;
		public string GetReflectionQuote() => App.Query(_reflectionQuote).FirstOrDefault()?.Text;
		public string GetReflectionQuoteSource() => App.Query(_reflectionQuoteSource).FirstOrDefault()?.Text;
		public string GetReflectionThought() => App.Query(_reflectionThought).FirstOrDefault()?.Text;
		public string GetReflectionCopyright() => App.Query(_reflectionCopyright).FirstOrDefault()?.Text;
	}
}
