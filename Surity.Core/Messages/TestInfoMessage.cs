using Newtonsoft.Json;
using System;

namespace Surity
{
	[Serializable]
	public class TestInfoMessage : IMessage
	{
		public string category;
		public string name;

		[JsonConstructor]
		public TestInfoMessage(string category, string name)
		{
			this.category = category;
			this.name = name;
		}
	}
}
