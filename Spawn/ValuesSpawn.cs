using System.ComponentModel;
using UnityEngine;

namespace Nox.CCK.Worlds.Spawns {
	[DisplayName("Values Spawn")]
	public class ValuesSpawn : SpawnBehavior {
		public Vector3    position = Vector3.zero;
		public Quaternion rotation = Quaternion.identity;

		public override Vector3 Position
			=> position;

		public override Quaternion Rotation
			=> rotation;
	}
}
