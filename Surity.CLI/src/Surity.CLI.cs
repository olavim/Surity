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

			Console.CancelKeyPress += OnExit;
			Console.InputEncoding = Encoding.UTF8;
			Console.OutputEncoding = Encoding.UTF8;

			if (args.Any(s => s == "--version" || s == "-v"))
			{
				AnsiConsole.WriteLine(ThisAssembly.Project.Version);
				return 0;
			}

			var app = new CommandApp<RunTestsCommand>();

#if DEBUG
			app.Configure(config => config.PropagateExceptions());
#endif

			try
			{
				return app.Run(args);
			}
			catch (Exception ex)
			{
				StackTraceFormatter.PrintStackTrace(new TestError(ex), new TypePattern("Spectre.Console.Cli.* | System.*"), true);
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
