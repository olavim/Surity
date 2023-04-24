using System;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;

namespace Surity
{
	public class AdapterListener : IDisposable
	{
		public const int LISTENER_PORT = 45750;
		public const byte MESSAGE_TESTRESULT = 0;
		public const byte MESSAGE_FINISH = 1;

		private readonly Socket listener;
		private readonly ManualResetEvent connectionEvent;
		private Socket connection;
		private MessageClient messageClient;

		public AdapterListener()
		{
			this.connectionEvent = new ManualResetEvent(false);
			this.listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			this.listener.Bind(new IPEndPoint(IPAddress.Parse("127.0.0.1"), LISTENER_PORT));
			this.listener.Listen(1);
		}

		public void WaitForClient(WaitHandle cancelToken)
		{
			this.connectionEvent.Reset();

			var ar = this.listener.BeginAccept(new AsyncCallback(this.OnConnection), this.listener);
			WaitHandle.WaitAny(new WaitHandle[] { cancelToken, this.connectionEvent });

			if (ar.IsCompleted)
			{
				this.connection = this.listener.EndAccept(ar);
				this.messageClient = new MessageClient(this.connection);
			}
		}

		public IMessage ReceiveMessage(WaitHandle cancelToken)
		{
			if (this.connection == null)
			{
				return new FinishMessage("Connection closed");
			}

			return this.messageClient.ReceiveMessage(cancelToken);
		}

		public void SendMessage(IMessage message)
		{
			this.messageClient.SendMessage(message);
		}

		private void OnConnection(IAsyncResult ar)
		{
			this.connectionEvent.Set();
		}

		public void Dispose()
		{
			this.listener.Dispose();
		}
	}
}
