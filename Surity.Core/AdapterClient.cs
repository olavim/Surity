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
			this.socket.Connect(IPAddress.Parse("127.0.0.1"), AdapterListener.MESSAGE_PORT);
		}

		public void SendTestResult(TestResult message)
		{
			this.socket.Send(GetMessageBytes(message), SocketFlags.None);
		}

		private byte[] GetMessageBytes(TestResult message)
		{
			var messageBytes = message.Serialize();
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
