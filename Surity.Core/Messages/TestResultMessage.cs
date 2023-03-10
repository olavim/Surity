using System.IO;

namespace Surity
{
	public class TestResultMessage : Message
	{
		public TestResult result;

		public TestResultMessage(TestResult result)
		{
			this.result = result;
		}

		protected override byte[] Serialize()
		{
			using (MemoryStream m = new MemoryStream())
			{
				using (BinaryWriter writer = new BinaryWriter(m))
				{
					writer.Write(this.result.testCategory ?? "");
					writer.Write(this.result.testName ?? "");
					writer.Write(this.result.pass);
					writer.Write(this.result.message ?? "");
				}
				return m.ToArray();
			}
		}

		protected override void Restore(BinaryReader reader)
		{
			this.result = new TestResult
			{
				testCategory = reader.ReadString(),
				testName = reader.ReadString(),
				pass = reader.ReadBoolean(),
				message = reader.ReadString()
			};
		}
	}
}
