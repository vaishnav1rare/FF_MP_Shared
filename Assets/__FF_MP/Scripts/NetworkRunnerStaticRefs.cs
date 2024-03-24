using Fusion;

namespace OneRare.FoodFury.Multiplayer
{
	public static class NetworkRunnerStaticRefs
	{
		public static LevelManager GetLevelManager(this NetworkRunner runner) => runner ? (LevelManager)runner.SceneManager : null;
	}
}