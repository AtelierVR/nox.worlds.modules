using UnityEngine;

namespace Nox.Worlds.Spawns {
	/// <summary>
	/// Represents a spawn point within the world.
	/// </summary>
	public interface ISpawn {
		/// <summary>
		/// The position of the spawn point in world coordinates.
		/// </summary>
		public Vector3 Position { get; }

		/// <summary>
		/// The rotation of the spawn point as a quaternion.
		/// </summary>
		public Quaternion Rotation { get; }
	}
}
