using BepInEx;

namespace Surity
{
	[BepInPlugin(ModId, ModName, ModVersion)]
	public sealed class SurityPlugin : BaseUnityPlugin
	{
		public const string ModId = "io.olavim.surity.bepinex";
		public const string ModName = "Surity.BepInEx";
		public const string ModVersion = ThisAssembly.Project.Version;

		private AdapterClient client;

		private void Start()
		{
			if (TestRunner.IsTestClient)
			{
				this.client = new AdapterClient();
				Debug.SetLogger(new DebugLogger(this.client));
				this.StartCoroutine(TestRunner.RunTestsAndExit(this.client));
			}
		}

		private void OnDisable()
		{
			this.client?.SendFinishMessage();
			this.client?.Dispose();
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
