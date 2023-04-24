using System;
using System.Diagnostics;

namespace Surity
{
	[Serializable]
	public class StackFrameInfo
	{
		public MethodDetails Method { get; set; }
		public string FileName { get; set; }
		public int LineNumber { get; set; }

		public StackFrameInfo(EnhancedStackFrame frame)
		{
			if (frame.MethodInfo != null && !string.IsNullOrEmpty(frame.MethodInfo.Name))
			{
				this.Method = new MethodDetails(frame.MethodInfo);
			}

			this.FileName = frame.GetFileName() ?? string.Empty;
			this.LineNumber = frame.GetFileLineNumber();
		}

		public StackFrameInfo(MethodDetails method, string fileName, int lineNumber)
		{
			this.Method = method;
			this.FileName = fileName;
			this.LineNumber = lineNumber;
		}

		private StackFrameInfo() { }
	}
}