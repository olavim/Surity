using System;

namespace Surity
{
	[Serializable]
	public class FinishMessage : IMessage
	{
		public string Reason { get; set; }

		public FinishMessage(string reason)
		{
			this.Reason = reason;
		}

		public FinishMessage() { }
	}
}
