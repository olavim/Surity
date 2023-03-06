using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Surity
{
	public static class Surity
	{
		private static int Main(string[] args)
		{
			var forwardArgs = new List<string>();
			int doubleDashIndex = Array.IndexOf(args, "--");
			if (doubleDashIndex != -1)
			{
				forwardArgs.AddRange(args.Skip(doubleDashIndex + 1));
			}

			return Surity.RunExecutable(args[0], forwardArgs.ToArray());
		}

		private static int RunExecutable(string source, string[] forwardArgs)
		{
			var processArgs = new List<string>() {
				"-batchmode",
				"-nographics",
				"-runSurityTests"
			};

			processArgs.AddRange(forwardArgs);

			var psi = new ProcessStartInfo
			{
				FileName = source,
				Arguments = string.Join(" ", processArgs),
				CreateNoWindow = true,
				RedirectStandardOutput = false,
				RedirectStandardError = false,
				UseShellExecute = false
			};

			var testResults = new List<TestResult>();
			var failedTestResults = new List<TestResult>();

			using var listener = new AdapterListener();
			using var process = Process.Start(psi);

			Console.WriteLine("Waiting for test adapter...");

			listener.WaitForClient();

			Console.WriteLine("Test run started");

			TestResult result;
			string category = "";

			while ((result = listener.ReceiveTestResult()) != null)
			{
				testResults.Add(result);

				if (result.testCategory != category)
				{
					category = result.testCategory;
					Surity.PrintCategory(category);
				}

				Surity.PrintTestResult(result);

				if (!result.pass)
				{
					failedTestResults.Add(result);
				}
			}

			foreach (var failedResult in failedTestResults)
			{
				Surity.PrintTestResultMessage(failedResult);
			}

			Surity.PrintSummary(testResults);

			process.WaitForExit();

			return failedTestResults.Count == 0 ? 0 : 1;
		}

		private static void PrintCategory(string category)
		{
			Console.ForegroundColor = ConsoleColor.White;
			Console.WriteLine($"\n{category}\n");
			Console.ResetColor();
		}

		private static void PrintTestResult(TestResult result)
		{
			Console.Write("\t");
			Console.ForegroundColor = result.pass ? ConsoleColor.Green : ConsoleColor.Red;
			Console.Write(result.pass ? "\u221A " : "X ");
			Console.ForegroundColor = ConsoleColor.DarkGray;
			Console.Write($"{result.testName}\n");
			Console.ResetColor();
		}

		private static void PrintTestResultMessage(TestResult result)
		{
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine($"\n{result.testCategory} \u203A {result.testName}\n");
			Console.ForegroundColor = ConsoleColor.DarkGray;
			Console.WriteLine(result.message);
			Console.ResetColor();
		}

		private static void PrintSummary(List<TestResult> results)
		{
			var groups = results.GroupBy(r => r.testCategory);
			int passedCategoryCount = groups.Count(g => g.All(result => result.pass));
			int failedCategoryCount = groups.Count() - passedCategoryCount;
			int passedTestCount = results.Count(r => r.pass);
			int failedTestCount = results.Count - passedTestCount;

			Console.ForegroundColor = ConsoleColor.White;
			Console.Write("\nTest Classes: ");

			if (failedCategoryCount > 0)
			{
				Console.ForegroundColor = ConsoleColor.Red;
				Console.Write($"{failedCategoryCount} failed");
				Console.ForegroundColor = ConsoleColor.White;
				Console.Write(", ");
			}

			if (passedCategoryCount > 0)
			{
				Console.ForegroundColor = ConsoleColor.Green;
				Console.Write($"{passedCategoryCount} passed");
				Console.ForegroundColor = ConsoleColor.White;
				Console.Write(", ");
			}

			Console.Write($"{passedCategoryCount + failedCategoryCount} total\n");
			Console.Write("Tests:        ");

			if (failedTestCount > 0)
			{
				Console.ForegroundColor = ConsoleColor.Red;
				Console.Write($"{failedTestCount} failed");
				Console.ForegroundColor = ConsoleColor.White;
				Console.Write(", ");
			}

			if (passedTestCount > 0)
			{
				Console.ForegroundColor = ConsoleColor.Green;
				Console.Write($"{passedTestCount} passed");
				Console.ForegroundColor = ConsoleColor.White;
				Console.Write(", ");
			}

			Console.Write($"{passedTestCount + failedTestCount} total\n");

			Console.ForegroundColor = ConsoleColor.DarkGray;
			Console.WriteLine("Test run finished");

			Console.ResetColor();
		}
	}
}
