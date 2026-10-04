using System;
using Nox.Worlds.Spawns;
using UnityEngine;

namespace Nox.CCK.Worlds.Spawns {
	/// <summary>
	/// Spawn défini par des valeurs. C'est le type attendu par <c>SpawnsWorldModule.spawns</c>, un
	/// tableau de références managées (<c>[SerializeReference]</c>) : Unity y sérialise les champs de
	/// l'objet, pas une référence d'objet, donc les composants <see cref="SpawnBehavior"/> ne peuvent
	/// pas y figurer.
	/// </summary>
	[Serializable]
	public class StructSpawn : ISpawn {
		public Vector3    position = Vector3.zero;
		public Quaternion rotation = Quaternion.identity;

		/// <summary>Utilisé par la désérialisation des références managées.</summary>
		public StructSpawn() { }

		public StructSpawn(Transform transform)
			: this(transform.position, transform.rotation) { }

		public StructSpawn(Vector3 position, Quaternion rotation) {
			this.position = position;
			this.rotation = rotation;
		}

		public Vector3 Position
			=> position;

		public Quaternion Rotation
			=> rotation;
	}
}
