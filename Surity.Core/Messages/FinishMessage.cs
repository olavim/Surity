using System.IO;

namespace Surity
{
	public class FinishMessage : Message
	{
		protected override byte[] Serialize() => new byte[] { };
		protected override void Restore(BinaryReader reader) { }
	}
}
