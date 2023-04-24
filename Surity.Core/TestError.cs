using System;
using System.Diagnostics;
using System.Linq;

namespace Surity
{
	[Serializable]
	public class TestError
	{
		public string Name { get; set; }
		public string Message { get; set; }
		public TestError InnerError { get; set; }
		public StackFrameInfo[] StackFrames { get; set; }

		public TestError(Exception exception)
		{
			var exceptionType = exception.GetType();
			this.Name = exceptionType.FullName ?? exceptionType.Name;

			this.Message = exception.Message;
			this.InnerError = exception.InnerException == null ? null : new TestError(exception.InnerException);

			var stackTrace = new EnhancedStackTrace(exception);
			this.StackFrames = stackTrace.GetFrames().Select(f => new StackFrameInfo((EnhancedStackFrame) f)).ToArray();
		}

		private TestError() { }
	}
}
