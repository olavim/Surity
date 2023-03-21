using System;

namespace Surity
{
	[Serializable]
	public class TestInfo
	{
		public string category;
		public string name;

		public TestInfo(string category, string name)
		{
			this.category = category;
			this.name = name;
		}
	}
}
