using BepInEx;
using System.Collections;

namespace Surity
{
	[BepInPlugin(ModId, ModName, ModVersion)]
	public sealed class SurityPlugin : BaseUnityPlugin
	{
		public const string ModId = "io.olavim.surity.bepinex";
		public const string ModName = "Surity.BepInEx";
		public const string ModVersion = ThisAssembly.Project.Version;

		private void Start()
		{
			if (TestRunner.IsTestClient)
			{
				this.StartCoroutine(this.RunTestsAndExit());
			}
		}

		private IEnumerator RunTestsAndExit()
		{
			using (var client = new AdapterClient())
			{
				Debug.SetLogger(new DebugLogger(client));
				yield return TestRunner.RunTestsAndExit(client);
			}
		}
	}

	internal class DebugLogger : ILogger
	{
		private readonly AdapterClient client;

		public DebugLogger(AdapterClient client)
		{
			this.client = client;
		}

		public void Log(string message)
		{
			this.client.SendDebugMessage(message);
		}
	}
}
