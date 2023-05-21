using System;
using System.Linq;
using System.Text;
using System.Threading;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Surity
{
	internal static class Program
	{
		public static bool exitRequested;
		public static readonly ManualResetEvent ExitEvent = new ManualResetEvent(false);

		private static int Main(string[] args)
		{
			Debug.SetLogger(new DebugLogger());

			try
			{
				Console.CancelKeyPress += OnExit;
				Console.InputEncoding = Encoding.UTF8;
				Console.OutputEncoding = Encoding.UTF8;
			}
			catch (Exception)
			{
				// Ignore
			}

			if (args.Any(s => s == "--version" || s == "-v"))
			{
				AnsiConsole.WriteLine(ThisAssembly.Project.Version);
				return 0;
			}

			var app = new CommandApp<RunTestsCommand>();
			app.Configure(config => config.PropagateExceptions());

			try
			{
				return app.Run(args);
			}
			catch (Exception ex)
			{
				AnsiConsole.MarkupLine("\n[red]An internal error occurred during test execution[/]\n");

#if DEBUG
				StackTraceFormatter.PrintStackTrace(new TestError(ex), new TypePattern("Spectre.* | System.*"), false);
#else
				AnsiConsole.MarkupLineInterpolated($"[grey]Error:[/] {ex.Message}");
#endif
				return 1;
			}
		}

		private static void OnExit(object sender, ConsoleCancelEventArgs args)
		{
			args.Cancel = true;
			exitRequested = true;
			ExitEvent.Set();
		}

		private class DebugLogger : ILogger
		{
			public void Log(string message)
			{
				foreach (string line in message.Split('\n'))
				{
					AnsiConsole.WriteLine(line);
				}
			}
		}
	}
}
