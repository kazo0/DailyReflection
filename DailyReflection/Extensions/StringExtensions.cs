﻿using System.Text.RegularExpressions;
using System.Web;

namespace DailyReflection.Extensions
{
	public static class StringExtensions
	{
		public static string StripHtml(this string input) => Regex.Replace(HttpUtility.HtmlDecode(input), "<.*?>", string.Empty);
	}
}
