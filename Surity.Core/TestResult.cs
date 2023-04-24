using System;

namespace Surity
{
	[Serializable]
	public class TestResult
	{
		public string TestName { get; set; }
		public string TestCategory { get; set; }
		public ExecutionResult Result { get; set; }

		public TestResult(string testName, string testCategory, ExecutionResult result)
		{
			this.TestName = testName;
			this.TestCategory = testCategory;
			this.Result = result;
		}

		private TestResult() { }
	}
}
