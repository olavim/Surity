using System;

namespace Surity
{
	[Serializable]
	public class TestResult
	{
		public TestInfo testInfo;
		public ExecutionResult result;

		public TestResult(TestInfo testInfo, ExecutionResult result)
		{
			this.testInfo = testInfo;
			this.result = result;
		}
	}
}
