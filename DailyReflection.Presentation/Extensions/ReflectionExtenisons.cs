using DailyReflection.Data.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DailyReflection.Presentation.Extensions
{
	public static class ReflectionExtenisons
	{
		public static string ToDisplayString(this Reflection reflection)
		{
			if (reflection == null)
			{ 
				return null; 
			}

			return $"{reflection.Title}\n\n{reflection.Reading}\n— {reflection.Source}\n\n{reflection.Thought}".StripHtml();
		}
	}
}
