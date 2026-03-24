using Nox.Worlds.Spawns;
using UnityEngine;

namespace Nox.CCK.Worlds.Spawns {
	public abstract class SpawnBehavior : MonoBehaviour, ISpawn {
		public abstract Vector3    Position { get; }
		public abstract Quaternion Rotation { get; }
	}
}
