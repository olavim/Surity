using System;

namespace Surity
{
	[Serializable]
	public class TestResult
	{
		public string testName;
		public string testCategory;
		public ExecutionResult result;

		public TestResult(string testName, string testCategory, ExecutionResult result)
		{
			this.testName = testName;
			this.testCategory = testCategory;
			this.result = result;
		}
	}
}
