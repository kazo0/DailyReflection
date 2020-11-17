using System;
using System.Collections.Generic;
using System.Text;
using DailyReflection.Extensions;

namespace DailyReflection.Models
{
	public class Reflection
	{
		public string Title { get; set; }
		public string Quote { get; set; }
		public string QuoteSource { get; set; }
		public string Thought { get; set; }
		public string Copyright { get; set; }

		public override string ToString()
		{
			return $"{Title}\n\n{Quote}\n{QuoteSource}\n\n{Thought}".StripHtml();
		}
	}
}
