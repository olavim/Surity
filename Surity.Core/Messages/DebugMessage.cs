using System;

namespace Surity
{
	[Serializable]
	public class DebugMessage : IMessage
	{
		public string Message { get; set; }

		public DebugMessage(string message)
		{
			this.Message = message;
		}

		public DebugMessage() { }
	}
}
