using Newtonsoft.Json;
using System;
using System.Diagnostics;

namespace Surity
{
	[Serializable]
	public class StackFrameInfo
	{
		public MethodDetails Method { get; }
		public string FileName { get; }
		public int LineNumber { get; }

		public StackFrameInfo(EnhancedStackFrame frame)
		{
			if (frame.MethodInfo != null && !string.IsNullOrEmpty(frame.MethodInfo.Name))
			{
				this.Method = new MethodDetails(frame.MethodInfo);
			}

			this.FileName = frame.GetFileName() ?? string.Empty;
			this.LineNumber = frame.GetFileLineNumber();
		}

		[JsonConstructor]
		public StackFrameInfo(MethodDetails method, string fileName, int lineNumber)
		{
			this.Method = method;
			this.FileName = fileName;
			this.LineNumber = lineNumber;
		}
	}
}