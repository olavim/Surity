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
		private readonly MessageClient messageClient;

		public AdapterClient()
		{
			this.socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			this.socket.Connect(IPAddress.Parse("127.0.0.1"), AdapterListener.LISTENER_PORT);
			this.messageClient = new MessageClient(this.socket);
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
			this.messageClient.SendMessage(message);
			this.messageClient.ReceiveMessage();
		}

		public void Dispose()
		{
			this.socket.Dispose();
		}
	}
}
