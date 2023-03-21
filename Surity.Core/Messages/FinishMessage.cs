using Newtonsoft.Json;
using System;

namespace Surity
{
	[Serializable]
	public class FinishMessage : IMessage
	{
		public string reason;

		[JsonConstructor]
		public FinishMessage(string reason)
		{
			this.reason = reason;
		}
	}
}
