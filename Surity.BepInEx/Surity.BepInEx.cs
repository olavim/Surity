using BepInEx;

namespace Surity
{
	[BepInPlugin(ModId, ModName, ModVersion)]
	public class SurityPlugin : BaseUnityPlugin
	{
		public const string ModId = "io.olavim.surity.bepinex";
		public const string ModName = "Surity.BepInEx";
		public const string ModVersion = "1.0.0";

		private void Start()
		{
			if (TestRunner.IsTestClient)
			{
				this.StartCoroutine(TestRunner.RunTestsAndExit());
			}
		}
	}
}
