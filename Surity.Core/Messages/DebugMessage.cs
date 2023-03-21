using Newtonsoft.Json;
using System;

namespace Surity
{
	[Serializable]
	public class DebugMessage : IMessage
	{
		public string message;

		[JsonConstructor]
		public DebugMessage(string message)
		{
			this.message = message;
		}
	}
}
