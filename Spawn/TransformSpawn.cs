using System;
using System.ComponentModel;
using Nox.Worlds.Spawns;
using UnityEngine;

namespace Nox.CCK.Worlds.Spawns {
	/// <summary>
	/// Spawn placé sur un objet de la scène : la position et la rotation sont celles du transform visé.
	/// C'est une valeur, pas un composant : <c>SpawnsWorldModule.spawns</c> est un tableau de références
	/// managées, où seule une valeur peut figurer — une référence d'objet se place dans un champ de
	/// cette valeur.
	/// </summary>
	[Serializable]
	[DisplayName("Transform Spawn")]
	public class TransformSpawn : ISpawn {
		public Transform transform;

		public Vector3 Position
			=> transform ? transform.position : Vector3.zero;

		public Quaternion Rotation
			=> transform ? transform.rotation : Quaternion.identity;
	}
}
