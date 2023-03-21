using Newtonsoft.Json;
using System;

namespace Surity
{
	[Serializable]
	public class TestResultMessage : IMessage
	{
		public TestResult result;

		[JsonConstructor]
		public TestResultMessage(TestResult result)
		{
			this.result = result;
		}
	}
}
