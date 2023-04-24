using System;

namespace Surity
{
	[Serializable]
	public class TestInfoMessage : IMessage
	{
		public string Category { get; set; }
		public string Name { get; set; }

		public TestInfoMessage(string category, string name)
		{
			this.Category = category;
			this.Name = name;
		}

		public TestInfoMessage() { }
	}
}
