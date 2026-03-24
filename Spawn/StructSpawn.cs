using Nox.Worlds.Spawns;
using UnityEngine;

namespace Nox.CCK.Worlds.Spawns {
	public class StructSpawn : ISpawn {
		public StructSpawn(Transform transform)
			: this(transform.position, transform.rotation) { }

		public StructSpawn(Vector3 position, Quaternion rotation) {
			Position = position;
			Rotation = rotation;
		}
		
		public Vector3 Position { get; }

		public Quaternion Rotation { get; }
	}
}
