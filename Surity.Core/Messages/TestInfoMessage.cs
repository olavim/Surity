using Newtonsoft.Json;
using System;

namespace Surity
{
	[Serializable]
	public class TestInfoMessage : IMessage
	{
		public TestInfo info;

		[JsonConstructor]
		public TestInfoMessage(TestInfo info)
		{
			this.info = info;
		}
	}
}
