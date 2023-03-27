using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Surity
{
	internal class MessageClient
	{
		private readonly Socket socket;
		private readonly ManualResetEvent receiveEvent;
		private readonly List<byte> receiveBuffer = new List<byte>();

		private int receiveLength;

		public MessageClient(Socket socket)
		{
			this.socket = socket;
			this.receiveEvent = new ManualResetEvent(false);
		}

		public IMessage ReceiveMessage(WaitHandle cancelToken = null)
		{
			this.receiveEvent.Reset();

			var buffer = new byte[4096];
			int received;
			var ar = this.socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(this.OnReceive), this.socket);

			if (cancelToken == null)
			{
				this.receiveEvent.WaitOne();
			}
			else
			{
				WaitHandle.WaitAny(new WaitHandle[] { cancelToken, this.receiveEvent });
			}

			if (ar.IsCompleted)
			{
				received = this.socket.EndReceive(ar);
			}
			else
			{
				return new FinishMessage("Cancelled");
			}

			this.receiveBuffer.AddRange(buffer.Take(received));

			if (this.receiveLength == 0 && this.receiveBuffer.Count >= 4)
			{
				this.receiveLength = BitConverter.ToInt32(this.receiveBuffer.GetRange(0, 4).ToArray(), 0);
				this.receiveBuffer.RemoveRange(0, 4);
			}

			if (this.receiveLength > 0 && this.receiveBuffer.Count >= this.receiveLength)
			{
				var messageBytes = this.receiveBuffer.GetRange(0, this.receiveLength).ToArray();
				string messageJson = Encoding.UTF8.GetString(messageBytes);
				var message = (IMessage) JsonConvert.DeserializeObject(messageJson, new JsonSerializerSettings
				{
					TypeNameHandling = TypeNameHandling.All,
					ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
					ContractResolver = new PrivateResolver()
				});

				this.receiveBuffer.RemoveRange(0, this.receiveLength);
				this.receiveLength = 0;

				return message;
			}

			return null;
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

		private void OnReceive(IAsyncResult ar)
		{
			this.receiveEvent.Set();
		}
	}
}
