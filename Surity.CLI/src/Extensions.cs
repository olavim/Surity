using Spectre.Console;
using System;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Surity
{
	internal static class Extensions
	{
		public static StringBuilder AppendWithStyle(this StringBuilder builder, Style style, int value)
		{
			return AppendWithStyle(builder, style, value.ToString(CultureInfo.InvariantCulture));
		}

		public static StringBuilder AppendWithStyle(this StringBuilder builder, Style style, string value)
		{
			value ??= string.Empty;

			if (style != null)
			{
				return builder
					.Append('[')
					.Append(style.ToMarkup())
					.Append(']')
					.Append(value.EscapeMarkup())
					.Append("[/]");
			}

			return builder.Append(value);
		}

		public static bool TryGetUri(this string path, out Uri result)
		{
			try
			{
				if (!Uri.TryCreate(path, UriKind.Absolute, out var uri))
				{
					result = null;
					return false;
				}

				if (uri.Scheme == "file")
				{
					var builder = new UriBuilder(uri)
					{
						Host = Dns.GetHostName(),
					};

					uri = builder.Uri;
				}

				result = uri;
				return true;
			}
			catch
			{
				result = null;
				return false;
			}
		}

		public static bool Like(this string str, string pattern)
		{
			string regexPattern = "^" + Regex.Escape(pattern).Replace(@"\*", ".*").Replace(@"\?", ".") + "$";
			return new Regex(regexPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline).IsMatch(str);
		}
	}
}
