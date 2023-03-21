using Newtonsoft.Json;
using System;

namespace Surity
{
	[Serializable]
	public sealed class ExecutionResult
	{
		public static ExecutionResult Pass()
		{
			return new ExecutionResult(true);
		}

		public static ExecutionResult Fail(Exception exception)
		{
			return Fail(new TestError(exception));
		}

		public static ExecutionResult Fail(TestError error)
		{
			return new ExecutionResult(false, error);
		}

		public readonly bool pass;
		public readonly TestError error;

		private ExecutionResult(bool pass) : this(pass, null) { }

		[JsonConstructor]
		private ExecutionResult(bool pass, TestError error)
		{
			this.pass = pass;
			this.error = error;
		}
	}
}
