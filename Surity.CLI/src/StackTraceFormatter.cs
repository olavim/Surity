using Spectre.Console;
using Spectre.Console.Rendering;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Surity
{
	internal static class StackTraceFormatter
	{
		private class StackTraceSettings
		{
			public StackTraceFormats Format { get; set; }
			public TypePattern Filter { get; set; }
			public StackTraceStyle Style { get; set; }
		}

		private class StackTraceStyle
		{
			public Style ErrorName { get; set; }
			public Style ErrorMessage { get; set; }
			public Style NonEmphasized { get; set; }
			public Style Parenthesis { get; set; }
			public Style Method { get; set; }
			public Style ParameterName { get; set; }
			public Style ParameterType { get; set; }
			public Style Path { get; set; }
			public Style LineNumber { get; set; }
			public Style Dimmed { get; set; }
		}

		[Flags]
		private enum StackTraceFormats
		{
			Default = 0,
			ShortenTypes = 1 << 0,
			ShortenMethods = 1 << 1,
			StartFromOrigin = 1 << 2,
		}

		public static void PrintStackTrace(TestError error, TypePattern filter, bool shortTypeNames)
		{
			StackTraceFormats format = StackTraceFormats.Default;

			if (shortTypeNames)
			{
				format |= StackTraceFormats.ShortenTypes;
			}

			var settings = new StackTraceSettings
			{
				Format = format,
				Filter = filter,
				Style = new StackTraceStyle
				{
					ErrorName = new Style().Foreground(Color.Grey),
					ErrorMessage = new Style().Foreground(Color.White),
					NonEmphasized = new Style().Foreground(Color.Grey),
					Parenthesis = new Style().Foreground(Color.White),
					Method = new Style().Foreground(Color.White),
					ParameterName = new Style().Foreground(Color.White),
					ParameterType = new Style().Foreground(Color.Teal),
					Path = new Style().Foreground(Color.White),
					LineNumber = new Style().Foreground(Color.White),
					Dimmed = new Style().Foreground(Color.Grey)
				}
			};
			AnsiConsole.Write(RenderError(error, settings));
		}

		private static IRenderable RenderError(TestError error, StackTraceSettings settings)
		{
			if (error is null)
			{
				throw new ArgumentNullException(nameof(error));
			}

			var message = GetMessage(error, settings);
			var frames = GetStackFrames(error, settings);
			return new Rows(message, frames).Expand();
		}

		private static Markup GetMessage(TestError error, StackTraceSettings settings)
		{
			bool shortenTypes = (settings.Format & StackTraceFormats.ShortenTypes) != 0;
			var type = Emphasize(error.Name, new[] { '.' }, settings.Style.ErrorName, shortenTypes, settings);

			var message = $"[{settings.Style.ErrorMessage.ToMarkup()}]{error.Message.EscapeMarkup()}[/]";
			return new Markup(string.Concat(type, ": ", message));
		}

		private static Grid GetStackFrames(TestError error, StackTraceSettings settings)
		{
			var styles = settings.Style;

			var grid = new Grid();
			grid.AddColumn(new GridColumn().PadLeft(2).PadRight(0).NoWrap());
			grid.AddColumn(new GridColumn().PadLeft(1).PadRight(0));

			if (error.InnerError != null)
			{
				grid.AddRow(Text.Empty, RenderError(error.InnerError, settings));
			}

			var frames = error.StackFrames;

			foreach (var frame in frames)
			{
				var builder = new StringBuilder();

				bool shortenMethods = (settings.Format & StackTraceFormats.ShortenMethods) != 0;
				bool shortenTypes = (settings.Format & StackTraceFormats.ShortenTypes) != 0;
				var method = frame.Method;

				if (method == null)
				{
					continue;
				}

				if (settings.Filter != null && settings.Filter.Matches(method.DeclaringType.FullName))
				{
					continue;
				}

				if (frame.Method.IsAsync)
				{
					builder.Append("async ");
				}

				if (frame.Method.ReturnType != null)
				{
					string typeName = frame.Method.ReturnType.GetDisplayName(!shortenTypes);
					builder.AppendWithStyle(styles.ParameterType, typeName.EscapeMarkup());
					builder.Append(' ');
				}

				if (!shortenMethods)
				{
					string typeName = frame.Method.DeclaringType.GetDisplayName(!shortenTypes);
					builder.AppendWithStyle(styles.NonEmphasized, $"{typeName.EscapeMarkup()}.");
				}

				builder.AppendWithStyle(styles.Method, method.Name);
				builder.AppendWithStyle(styles.Parenthesis, "(");
				AppendParameters(builder, method.Parameters, settings);
				builder.AppendWithStyle(styles.Parenthesis, ")");

				if (!string.IsNullOrEmpty(frame.FileName))
				{
					builder.Append(' ');
					builder.AppendWithStyle(styles.Dimmed, "in");
					builder.Append(' ');

					builder.AppendWithStyle(styles.Path, MakeRelative(frame.FileName));

					if (frame.LineNumber != 0)
					{
						builder.AppendWithStyle(styles.Dimmed, ":");
						builder.AppendWithStyle(styles.LineNumber, frame.LineNumber);
					}
				}

				grid.AddRow($"[{styles.Dimmed.ToMarkup()}]at[/]", builder.ToString());
			}

			return grid;
		}

		private static string MakeRelative(string filePath)
		{
			string path = Path.GetRelativePath(Directory.GetCurrentDirectory(), filePath);
			return path.StartsWith("..") ? filePath : $".{Path.DirectorySeparatorChar}{path}";
		}

		private static string Emphasize(string input, char[] separators, Style color, bool compact, StackTraceSettings settings)
		{
			var builder = new StringBuilder();

			int index = input.LastIndexOfAny(separators);
			if (index != -1)
			{
				if (!compact)
				{
					builder.AppendWithStyle(settings.Style.NonEmphasized, input[..(index + 1)]);
				}

				builder.AppendWithStyle(color, input.Substring(index + 1, input.Length - index - 1));
			}
			else
			{
				builder.Append(input.EscapeMarkup());
			}

			return builder.ToString();
		}

		private static void AppendParameters(StringBuilder builder, ParameterDetails[] parameters, StackTraceSettings settings)
		{
			bool shortenTypes = (settings.Format & StackTraceFormats.ShortenTypes) != 0;
			string nonEmphasizedColor = settings.Style.NonEmphasized.ToMarkup();
			string typeColor = settings.Style.ParameterType.ToMarkup();
			string nameColor = settings.Style.ParameterName.ToMarkup();

			if (parameters != null)
			{
				var parameterStrings = parameters.Select(x =>
				{
					string typeName = x.Type.GetDisplayName(!shortenTypes).EscapeMarkup();
					string prefix = string.IsNullOrEmpty(x.Prefix) ? "" : $"[{nonEmphasizedColor}]{x.Prefix.EscapeMarkup()}[/] ";
					return $"{prefix}[{typeColor}]{typeName}[/] [{nameColor}]{x.Name.EscapeMarkup()}[/]";
				});

				builder.Append(string.Join(", ", parameterStrings));
			}
		}
	}
}
