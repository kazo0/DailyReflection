using System.Text.RegularExpressions;
using System.Web;

namespace DailyReflection.Extensions;

public static partial class StringExtensions
{
    public static string StripHtml(this string input) => HtmlTagRegex().Replace(HttpUtility.HtmlDecode(input), string.Empty);

    [GeneratedRegex("<.*?>")]
    private static partial Regex HtmlTagRegex();
}
