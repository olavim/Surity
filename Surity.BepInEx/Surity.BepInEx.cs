using BepInEx;

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
				this.StartCoroutine(TestRunner.RunTestsAndExit());
			}
		}
	}
}
