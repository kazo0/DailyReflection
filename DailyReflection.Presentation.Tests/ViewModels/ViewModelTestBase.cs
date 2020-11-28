using DailyReflection.Presentation.ViewModels;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DailyReflection.Presentation.Tests.ViewModels
{
	[TestFixture(Category = "View Model Tests")]
	public abstract class ViewModelTestBase<TViewModel> where TViewModel : ViewModelBase
	{
		protected TViewModel ViewModelUnderTest { get; private set; }
		protected abstract TViewModel GetViewModel();

		[SetUp]
		public virtual async Task Setup()
		{
			ViewModelUnderTest = GetViewModel();
		}
	}
}
