using System;

namespace Surity
{
	public static class Debug
	{
		private static ILogger logger;

		public static void SetLogger(ILogger logger)
		{
			Debug.logger = logger;
		}

		public static void Log(object message)
		{
			if (logger == null)
			{
				throw new Exception("Logger not set");
			}

			logger.Log(message?.ToString() ?? "<null>");
		}
	}
}