using System;
using System.IO;
using System.Runtime.Serialization;

namespace Surity
{
	public abstract class Message
	{
		public static byte[] Serialize(Message message)
		{
			var messageBytes = message.Serialize();

			using (MemoryStream m = new MemoryStream())
			{
				using (BinaryWriter writer = new BinaryWriter(m))
				{
					writer.Write(message.GetType().FullName);
				}
				var typeBytes = m.ToArray();

				var bytes = new byte[typeBytes.Length + messageBytes.Length];
				typeBytes.CopyTo(bytes, 0);
				messageBytes.CopyTo(bytes, typeBytes.Length);
				return bytes;
			}
		}

		public static Message Deserialize(byte[] bytes)
		{
			using (MemoryStream m = new MemoryStream(bytes))
			{
				using (BinaryReader reader = new BinaryReader(m))
				{
					string typeName = reader.ReadString();
					var type = Type.GetType(typeName);

					var message = (Message) FormatterServices.GetUninitializedObject(type);
					message.Restore(reader);
					return message;
				}
			}
		}

		protected abstract byte[] Serialize();
		protected abstract void Restore(BinaryReader reader);
	}
}
