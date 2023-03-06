using System.IO;

namespace Surity
{
	public class TestResult
	{
		public string testCategory;
		public string testName;
		public bool pass;
		public string message;

		public byte[] Serialize()
		{
			using (MemoryStream m = new MemoryStream())
			{
				using (BinaryWriter writer = new BinaryWriter(m))
				{
					writer.Write(this.testCategory ?? "");
					writer.Write(this.testName ?? "");
					writer.Write(this.pass);
					writer.Write(this.message ?? "");
				}
				return m.ToArray();
			}
		}

		public static TestResult Deserialize(byte[] data)
		{
			var result = new TestResult();
			using (MemoryStream m = new MemoryStream(data))
			{
				using (BinaryReader reader = new BinaryReader(m))
				{
					result.testCategory = reader.ReadString();
					result.testName = reader.ReadString();
					result.pass = reader.ReadBoolean();
					result.message = reader.ReadString();
				}
			}
			return result;
		}
	}
}
