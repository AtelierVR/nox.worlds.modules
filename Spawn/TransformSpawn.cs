using System.ComponentModel;
using UnityEngine;

namespace Nox.CCK.Worlds.Spawns {
	[DisplayName("Values Spawn")]
	public class TransformSpawn : SpawnBehavior {
		public override Vector3 Position
			=> transform.position;

		public override Quaternion Rotation
			=> transform.rotation;
	}
}
