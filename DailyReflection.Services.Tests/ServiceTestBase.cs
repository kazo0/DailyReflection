using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace DailyReflection.Services.Tests
{
	[TestFixture(Category = "Service Tests")]
	public abstract class ServiceTestBase<TService> where TService : class
	{
		protected TService ServiceUnderTest { get; private set; }

		protected abstract TService GetService();

		[SetUp]
		public virtual void Setup()
		{
			ServiceUnderTest = GetService();
		}
	}
}
