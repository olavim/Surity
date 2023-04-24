using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Xml.Serialization;

namespace Surity
{
	internal class MessageClient
	{
		private readonly Socket socket;
		private readonly ManualResetEvent receiveEvent;
		private readonly List<byte> receiveBuffer = new List<byte>();

		private int typeLength;
		private int messageLength;

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

			if (this.typeLength == 0 && this.receiveBuffer.Count >= 8)
			{
				this.typeLength = BitConverter.ToInt32(this.receiveBuffer.ToArray(), 0);
				this.messageLength = BitConverter.ToInt32(this.receiveBuffer.ToArray(), 4);
				this.receiveBuffer.RemoveRange(0, 8);
			}

			if (this.typeLength > 0 && this.receiveBuffer.Count >= this.typeLength + this.messageLength)
			{
				var typeBytes = this.receiveBuffer.GetRange(0, this.typeLength).ToArray();
				var messageBytes = this.receiveBuffer.GetRange(this.typeLength, this.messageLength).ToArray();

				var type = Type.GetType(Encoding.UTF8.GetString(typeBytes));
				var serializer = new XmlSerializer(type);
				var message = (IMessage) serializer.Deserialize(new MemoryStream(messageBytes));

				this.receiveBuffer.RemoveRange(0, this.typeLength + this.messageLength);
				this.typeLength = 0;
				this.messageLength = 0;

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
			var typeBytes = Encoding.UTF8.GetBytes(message.GetType().FullName);
			var typePrefixBytes = BitConverter.GetBytes(typeBytes.Length);
			var messageBytes = this.GetXmlBytes(message);
			var messagePrefixBytes = BitConverter.GetBytes(messageBytes.Length);

			int totalBytes = typePrefixBytes.Length + messagePrefixBytes.Length + typeBytes.Length + messageBytes.Length;
			int messagePrefixIndex = typePrefixBytes.Length;
			int typeIndex = messagePrefixIndex + messagePrefixBytes.Length;
			int messageIndex = typeIndex + typeBytes.Length;

			var bytes = new byte[totalBytes];
			typePrefixBytes.CopyTo(bytes, 0);
			messagePrefixBytes.CopyTo(bytes, messagePrefixIndex);
			typeBytes.CopyTo(bytes, typeIndex);
			messageBytes.CopyTo(bytes, messageIndex);
			return bytes;
		}

		private byte[] GetXmlBytes(IMessage message)
		{
			var serializer = new XmlSerializer(message.GetType());
			using (var stream = new MemoryStream())
			{
				using (var writer = new StreamWriter(stream))
				{
					serializer.Serialize(writer, message);
				}

				return stream.ToArray();
			}
		}

		private void OnReceive(IAsyncResult ar)
		{
			this.receiveEvent.Set();
		}
	}
}
