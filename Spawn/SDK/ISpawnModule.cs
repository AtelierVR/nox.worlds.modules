namespace Nox.Worlds.Spawns {
	public interface ISpawnModule : IWorldModule, ISpawnGroup {
		public uint GetSpawnIndex();

		public SpawnType GetSpawnType();

		public ISpawn ChoiceSpawn();
	}
}
