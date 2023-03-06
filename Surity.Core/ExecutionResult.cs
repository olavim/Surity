namespace Surity
{
	public sealed class ExecutionResult
	{
		public static ExecutionResult Pass()
		{
			return new ExecutionResult(true);
		}

		public static ExecutionResult Fail(string message)
		{
			return new ExecutionResult(false, message);
		}

		public readonly bool pass;
		public readonly string message;

		private ExecutionResult(bool pass) : this(pass, null) { }

		private ExecutionResult(bool pass, string failReason)
		{
			this.pass = pass;
			this.message = failReason;
		}
	}
}
