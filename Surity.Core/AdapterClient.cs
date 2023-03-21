using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Surity
{
	public class AdapterClient : IDisposable
	{
		private readonly Socket socket;

		public AdapterClient()
		{
			this.socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			this.socket.Connect(IPAddress.Parse("127.0.0.1"), AdapterListener.LISTENER_PORT);
		}

		public void SendTestInfo(TestInfo testInfo)
		{
			this.SendMessage(new TestInfoMessage(testInfo));
		}

		public void SendTestResult(TestResult result)
		{
			this.SendMessage(new TestResultMessage(result));
		}

		public void SendDebugMessage(string message)
		{
			this.SendMessage(new DebugMessage(message));
		}

		public void SendFinishMessage()
		{
			this.SendMessage(new FinishMessage("Test run finished"));
		}

		public void SendMessage(IMessage message)
		{
			this.socket.Send(this.GetMessageBytes(message), SocketFlags.None);
		}

		private byte[] GetMessageBytes(IMessage message)
		{
			string messageJson = JsonConvert.SerializeObject(message, new JsonSerializerSettings
			{
				TypeNameHandling = TypeNameHandling.All
			});
			var messageBytes = Encoding.UTF8.GetBytes(messageJson);
			var prefixBytes = BitConverter.GetBytes(messageBytes.Length);

			var bytes = new byte[prefixBytes.Length + messageBytes.Length];
			prefixBytes.CopyTo(bytes, 0);
			messageBytes.CopyTo(bytes, prefixBytes.Length);
			return bytes;
		}

		public void Dispose()
		{
			this.socket.Dispose();
		}
	}
}
