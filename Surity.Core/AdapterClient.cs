using System;
using System.Net;
using System.Net.Sockets;

namespace Surity
{
	public class AdapterClient : IDisposable
	{
		private readonly Socket socket;
		private readonly MessageClient messageClient;

		public AdapterClient()
		{
			this.socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			this.socket.Connect(IPAddress.Parse("127.0.0.1"), AdapterListener.LISTENER_PORT);
			this.messageClient = new MessageClient(this.socket);
		}

		public void SendTestInfo(string name, string category)
		{
			this.SendMessage(new TestInfoMessage(name, category));
		}

		public void SendTestResult(TestResult result)
		{
			this.SendMessage(new TestResultMessage(result));
		}

		public void SendDebugMessage(string message)
		{
			this.SendMessage(new DebugMessage(message));
		}

		public void SendFinishMessage(string message = "Test run finished")
		{
			this.SendMessage(new FinishMessage(message));
		}

		public void SendMessage(IMessage message)
		{
			this.messageClient.SendMessage(message);
			this.messageClient.ReceiveMessage();
		}

		public void Dispose()
		{
			this.socket.Dispose();
		}
	}
}
