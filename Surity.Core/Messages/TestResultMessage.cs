using System;

namespace Surity
{
	[Serializable]
	public class TestResultMessage : IMessage
	{
		public TestResult Result { get; set; }

		public TestResultMessage(TestResult result)
		{
			this.Result = result;
		}

		private TestResultMessage() { }
	}
}
