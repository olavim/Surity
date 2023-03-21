using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;

namespace Surity
{
	public class AdapterListener : IDisposable
	{
		public const int LISTENER_PORT = 45750;
		public const byte MESSAGE_TESTRESULT = 0;
		public const byte MESSAGE_FINISH = 1;

		private readonly Socket listener;
		private readonly List<byte> receiveBuffer = new List<byte>();
		private readonly ManualResetEvent connectionEvent;
		private readonly ManualResetEvent receiveEvent;
		private Socket connection;
		private int receiveLength;

		public AdapterListener()
		{
			this.connectionEvent = new ManualResetEvent(false);
			this.receiveEvent = new ManualResetEvent(false);
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
			}
		}

		public IMessage ReceiveMessage(WaitHandle cancelToken)
		{
			if (this.connection == null)
			{
				return new FinishMessage("Connection closed");
			}

			this.receiveEvent.Reset();

			var buffer = new byte[4096];
			int received;
			var ar = this.connection.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(this.OnReceive), this.connection);
			WaitHandle.WaitAny(new WaitHandle[] { cancelToken, this.receiveEvent });

			if (ar.IsCompleted)
			{
				received = this.connection.EndReceive(ar);
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

		private void OnConnection(IAsyncResult ar)
		{
			this.connectionEvent.Set();
		}

		private void OnReceive(IAsyncResult ar)
		{
			this.receiveEvent.Set();
		}

		public void Dispose()
		{
			this.listener.Dispose();
		}
	}

	// Allows deserializing into properties with private setters
	public class PrivateResolver : DefaultContractResolver
	{
		protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
		{
			var prop = base.CreateProperty(member, memberSerialization);
			if (!prop.Writable)
			{
				var property = member as PropertyInfo;
				prop.Writable = property?.GetSetMethod(true) != null;
			}
			return prop;
		}
	}
}
