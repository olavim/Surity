using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Surity
{
	internal sealed class TypePatternConverter : TypeConverter
	{
		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			if (value is string stringValue)
			{
				return new TypePattern(stringValue);
			}

			throw new NotSupportedException("Cannot convert value to TypePattern.");
		}
	}

	internal sealed class RunTestsCommand : Command<RunTestsCommand.Settings>
	{
		public sealed class Settings : CommandSettings
		{
			[Description("Path to the Unity game executable")]
			[CommandArgument(0, "<GameExecutablePath>")]
			public string ExePath { get; set; }

			[Description("Prints version information")]
			[CommandOption("-v|--version")]
			public bool Version { get; set; }

			const string FilterStackTracesExamples = @"

Examples:

  Remove stack frames originating from types that match ""Assertions.*"" or ""UnityEngine.*"":
    --filter-stacktraces ""Assertions.* | UnityEngine.*""

  Only include stack frames originating from types that match ""Assertions.*"" or ""UnityEngine.*""
    --filter-stacktraces ""!(Assertions.* | UnityEngine.*)""

  Same as above
    --filter-stacktraces ""!Assertions.* & !UnityEngine.*""
";

			[Description("Removes frames from error stack traces that originate from the specified types." + FilterStackTracesExamples)]
			[CommandOption("-f|--filter-stacktraces <PATTERN>")]
			[TypeConverter(typeof(TypePatternConverter))]
			public TypePattern StackTraceFilter { get; set; }

			[Description("Makes test error stack traces more compact by removing namespaces from type names")]
			[CommandOption("-c|--compact-stacktraces")]
			[DefaultValue(false)]
			public bool CompactStackTraces { get; set; }

			[Description("Removes some bells and whistles from output. Meant for use in environments with limited console functionality, such as CI.")]
			[CommandOption("-s|--simple-output")]
			[DefaultValue(false)]
			public bool SimpleOutput { get; set; }
		}

		public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
		{
			var testResults = new List<TestResult>();
			var failedTestResults = new List<TestResult>();

			try
			{
				Console.CursorVisible = false;
			}
			catch (IOException)
			{
				// Ignore
			}

			IMessage message;
			string finishReason = "Test run stopped unexpectedly";
			string category = "";

			using var listener = new AdapterListener();

			var processArgs = new List<string>() {
				"-batchmode",
				"-nographics",
				"-runSurityTests"
			};

			processArgs.AddRange(context.Remaining.Raw);

			var psi = new ProcessStartInfo
			{
				FileName = settings.ExePath,
				Arguments = string.Join(" ", processArgs),
				CreateNoWindow = true,
				RedirectStandardOutput = false,
				RedirectStandardError = false,
				UseShellExecute = false
			};

			string grey = settings.SimpleOutput ? "white" : "grey";

			using var process = Process.Start(psi);

			void HandleTests(StatusContext ctx = null)
			{
				listener.WaitForClient(Program.ExitEvent);

				while (true)
				{
					message = listener.ReceiveMessage(Program.ExitEvent);

					if (ctx != null && message is TestInfoMessage infoMessage)
					{
						ctx.Status($"Running test: [{grey}]{infoMessage.Category} \u203A {infoMessage.Name}[/]");
					}

					if (message is TestResultMessage resultMessage)
					{
						var result = resultMessage.Result;
						testResults.Add(result);

						if (result.TestCategory != category)
						{
							category = result.TestCategory;
							AnsiConsole.WriteLine();
							AnsiConsole.WriteLine(category);
							AnsiConsole.WriteLine();
						}

						string check = result.Result.IsPass ? "[lime]\u221A[/] " : "[red]X[/] ";
						AnsiConsole.MarkupLine($"    {check}[{grey}]{{0}}[/]", Markup.Escape(result.TestName));

						if (!result.Result.IsPass)
						{
							failedTestResults.Add(result);
						}
					}

					if (message is DebugMessage debugMessage)
					{
						Debug.Log(debugMessage.Message);
					}

					if (message is FinishMessage finishMessage)
					{
						finishReason = finishMessage.Reason;
						break;
					}

					if (message != null)
					{
						listener.SendMessage(new SyncMessage());
					}
				}
			}

			try
			{
				if (settings.SimpleOutput)
				{
					AnsiConsole.WriteLine("Waiting for test adapter...");
					HandleTests();
				}
				else
				{
					AnsiConsole.Status()
						.Spinner(Spinner.Known.Dots2)
						.SpinnerStyle(new Style(foreground: Color.Grey))
						.Start("Waiting for test adapter...", HandleTests);
				}

				foreach (var failedResult in failedTestResults)
				{
					AnsiConsole.MarkupLineInterpolated($"\n[red]{failedResult.TestCategory} \u203A {failedResult.TestName}[/]\n");
					StackTraceFormatter.PrintStackTrace(failedResult.Result.Error, settings.StackTraceFilter, settings.CompactStackTraces);
				}

				this.PrintSummary(testResults);
				AnsiConsole.MarkupLine($"[{grey}]{0}[/]", Markup.Escape(finishReason));
			}
			finally
			{
				process.Kill();
				process.WaitForExit();
			}

			if (failedTestResults.Count > 0)
			{
				return 1;
			}

			if (Program.exitRequested)
			{
				return 2;
			}

			return 0;
		}

		private void PrintSummary(List<TestResult> results)
		{
			var groups = results.GroupBy(r => r.TestCategory);
			int passedCategoryCount = groups.Count(g => g.All(result => result.Result.IsPass));
			int failedCategoryCount = groups.Count() - passedCategoryCount;
			int passedTestCount = results.Count(r => r.Result.IsPass);
			int failedTestCount = results.Count - passedTestCount;

			AnsiConsole.Write("\nTest Classes: ");

			if (failedCategoryCount > 0)
			{
				AnsiConsole.MarkupInterpolated($"[red]{failedCategoryCount} failed[/], ");
			}

			if (passedCategoryCount > 0)
			{
				AnsiConsole.MarkupInterpolated($"[lime]{passedCategoryCount} passed[/], ");
			}

			AnsiConsole.WriteLine($"{passedCategoryCount + failedCategoryCount} total");
			AnsiConsole.Write("Tests:        ");

			if (failedTestCount > 0)
			{
				AnsiConsole.MarkupInterpolated($"[red]{failedTestCount} failed[/], ");
			}

			if (passedTestCount > 0)
			{
				AnsiConsole.MarkupInterpolated($"[lime]{passedTestCount} passed[/], ");
			}

			AnsiConsole.WriteLine($"{passedTestCount + failedTestCount} total");
		}
	}
}