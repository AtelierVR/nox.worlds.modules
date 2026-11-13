using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.CCK.Development;
using Nox.CCK.Utils;
using Nox.Players;
using Nox.Sessions;
using Nox.Worlds;
using UnityEngine;
using Gizmos = Nox.CCK.Development.Gizmos;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.CCK.Worlds.FellInVoid {
	[Gizmos("cck.worlds.fell_in_void")]
	public class FellInVoidWorldModule : MonoBehaviour, ISessionModule, IGizmos {
		/// <summary>
		/// Height the module is created with, in world units: a player below it is respawned. Exposed so the
		/// editor tooling can reason about spawns before the module exists (see the module notifications of
		/// <c>nox.worlds.modules</c>).
		/// </summary>
		public const float DefaultFallThreshold = -100f;

		#region Internal

		public static bool Check(IWorldDescriptor descriptor) {
			var modules = descriptor.GetModules<FellInVoidWorldModule>();

			var module = modules.Length switch {
				1 => modules.FirstOrDefault(),
				0 => descriptor.Anchor.AddComponent<FellInVoidWorldModule>(),
				_ => null
			};

			if (!module) {
				Logger.LogError("Verify that the World prefab has a valid FellInVoidWorldModule component.");
				return false;
			}

			return true;
		}

		public UniTask<bool> Setup(IRuntimeWorld runtime)
			=> UniTask.FromResult(true);

		#endregion

		public float fallThreshold = DefaultFallThreshold;

		public IPlayer LocalPlayer;

		public void OnPlayerJoined(IPlayer player) {
			if (!player.IsLocal) return;
			LocalPlayer = player;
		}

		public void OnPlayerLeft(IPlayer player) {
			if (!player.IsLocal) return;
			LocalPlayer = null;
		}

		public void FixedUpdate() {
			if (LocalPlayer == null) return;
			if (LocalPlayer.Position.y < fallThreshold)
				LocalPlayer.Respawn();
		}

		#region Gizmos

		/// <summary>Half size of the square drawn on the threshold plane, in metres.</summary>
		private const float PlaneSize = 3f;

		/// <summary>Colour of the threshold plane.</summary>
		private static readonly Color PlaneColor = new(1f, 0.35f, 0.25f);

		/// <summary>Colour of the threshold plane while the module is selected.</summary>
		private static readonly Color SelectedColor = new(1f, 0.7f, 0.2f);

		/// <summary>
		/// Scene view glyphs. Public on purpose: <c>OnDrawGizmos</c> is a Unity message, the scene view calls it
		/// by itself — the <see cref="GizmosAttribute"/> only makes the glyphs pickable through
		/// <c>Nox.CCK.Editor.GizmosRenderer</c>. The threshold is a horizontal plane, drawn as its outline under
		/// the module, with a dashed line going down to it.
		/// </summary>
		public void OnDrawGizmos() {
			var origin = transform.position;

			// The threshold is a world altitude: the plane is drawn under the module, not around it.
			var center = new Vector3(origin.x, fallThreshold, origin.z);

			Gizmos.Color = gameObject.IsSelected() ? SelectedColor : PlaneColor;

			// Outline of the plane: a flat square in the XZ plane.
			Gizmos.DrawWireCube(center, new Vector3(PlaneSize * 2f, 0f, PlaneSize * 2f));

			// Dashed line from the module down to the plane: the drop the player has to make to be respawned.
			if (origin.y > fallThreshold)
				Gizmos.DrawDottedLine(origin, center);
		}

		/// <summary>Point the renderer tests for picking: the outline of the threshold plane.</summary>
		public Vector3[] GetPickPoints()
			=> new[] { new Vector3(transform.position.x, fallThreshold, transform.position.z) };

		#endregion
	}
}
