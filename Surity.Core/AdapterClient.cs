using System;
using System.Net;
using System.Net.Sockets;

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

		public void SendTestResult(TestResult result)
		{
			var message = new TestResultMessage(result);
			this.socket.Send(this.GetMessageBytes(message), SocketFlags.None);
		}

		public void SendFinishMessage()
		{
			this.socket.Send(new byte[] { AdapterListener.MESSAGE_FINISH }, SocketFlags.None);
		}

		private byte[] GetMessageBytes(Message message)
		{
			var messageBytes = Message.Serialize(message);
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
