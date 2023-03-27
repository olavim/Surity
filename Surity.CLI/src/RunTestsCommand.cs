using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
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
		}

		public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
		{
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

			var testResults = new List<TestResult>();
			var failedTestResults = new List<TestResult>();

			Console.CursorVisible = false;
			IMessage message;
			string finishReason = "Test run stopped unexpectedly";
			string category = "";
			bool error = false;

			using var listener = new AdapterListener();
			using var process = Process.Start(psi);

			try
			{
				AnsiConsole.Status()
					.Spinner(Spinner.Known.Dots2)
					.SpinnerStyle(new Style(foreground: Color.Grey))
					.Start("Waiting for test adapter...", ctx =>
					{
						listener.WaitForClient(Program.ExitEvent);

						while (true)
						{
							message = listener.ReceiveMessage(Program.ExitEvent);

							if (message is TestInfoMessage infoMessage)
							{
								var testInfo = infoMessage.info;
								ctx.Status($"Running test: [grey]{testInfo.category} \u203A {testInfo.name}[/]");
							}

							if (message is TestResultMessage resultMessage)
							{
								var result = resultMessage.result;
								var testInfo = result.testInfo;
								testResults.Add(result);

								if (testInfo.category != category)
								{
									category = testInfo.category;
									AnsiConsole.WriteLine();
									AnsiConsole.WriteLine(testInfo.category);
									AnsiConsole.WriteLine();
								}

								string check = result.result.pass ? "[lime]\u221A[/] " : "[red]X[/] ";
								AnsiConsole.MarkupLine($"    {check}[grey]{{0}}[/]", Markup.Escape(testInfo.name));

								if (!result.result.pass)
								{
									failedTestResults.Add(result);
								}
							}

							if (message is DebugMessage debugMessage)
							{
								Debug.Log(debugMessage.message);
							}

							if (message is FinishMessage finishMessage)
							{
								finishReason = finishMessage.reason;
								break;
							}

							if (message != null)
							{
								listener.SendMessage(new SyncMessage());
							}
						}
					});

				foreach (var failedResult in failedTestResults)
				{
					var testInfo = failedResult.testInfo;
					AnsiConsole.MarkupLineInterpolated($"\n[red]{testInfo.category} \u203A {testInfo.name}[/]\n");
					StackTraceFormatter.PrintStackTrace(failedResult.result.error, settings.StackTraceFilter, settings.CompactStackTraces);
				}

				this.PrintSummary(testResults);
				AnsiConsole.MarkupLine("[grey]{0}[/]", Markup.Escape(finishReason));
			}
			catch (Exception ex)
			{
				AnsiConsole.MarkupLine("\n[red]An internal error occurred during test execution[/]\n");
				StackTraceFormatter.PrintStackTrace(new TestError(ex), null, true);
			}
			finally
			{
				error = true;
				process.Kill();
				process.WaitForExit();
			}

			if (error || failedTestResults.Count > 0)
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
			var groups = results.GroupBy(r => r.testInfo.category);
			int passedCategoryCount = groups.Count(g => g.All(result => result.result.pass));
			int failedCategoryCount = groups.Count() - passedCategoryCount;
			int passedTestCount = results.Count(r => r.result.pass);
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