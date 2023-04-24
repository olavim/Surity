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

		public bool IsPass { get; set; }
		public TestError Error { get; set; }

		private ExecutionResult(bool pass) : this(pass, null) { }

		private ExecutionResult(bool pass, TestError error)
		{
			this.IsPass = pass;
			this.Error = error;
		}

		private ExecutionResult() { }
	}
}
