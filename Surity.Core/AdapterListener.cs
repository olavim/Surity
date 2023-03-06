using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace Surity
{
	public class AdapterListener : IDisposable
	{
		public const int MESSAGE_PORT = 45750;

		private readonly Socket listener;
		private Socket connection;
		private readonly List<byte> receiveBuffer = new List<byte>();
		private int receiveLength;

		public AdapterListener()
		{
			this.listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			this.listener.Bind(new IPEndPoint(IPAddress.Parse("127.0.0.1"), AdapterListener.MESSAGE_PORT));
			this.listener.Listen(1);
		}

		public void WaitForClient()
		{
			this.connection = this.listener.Accept();
		}

		public TestResult ReceiveTestResult()
		{
			byte[] buffer = new byte[4096];
			int received = this.connection.Receive(buffer, SocketFlags.None);
			this.receiveBuffer.AddRange(buffer.Take(received));

			if (this.receiveLength == 0 && this.receiveBuffer.Count >= 4)
			{
				this.receiveLength = BitConverter.ToInt32(this.receiveBuffer.GetRange(0, 4).ToArray(), 0);
				this.receiveBuffer.RemoveRange(0, 4);
			}

			if (this.receiveLength > 0 && this.receiveBuffer.Count >= this.receiveLength)
			{
				var messageBytes = this.receiveBuffer.GetRange(0, this.receiveLength).ToArray();
				var message = TestResult.Deserialize(messageBytes);

				this.receiveBuffer.RemoveRange(0, this.receiveLength);
				this.receiveLength = 0;

				return message;
			}

			return null;
		}

		public void Dispose()
		{
			this.listener.Dispose();
		}
	}
}
