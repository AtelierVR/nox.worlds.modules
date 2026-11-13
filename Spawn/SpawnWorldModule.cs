using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.CCK.Build;
using Nox.CCK.Development;
using Nox.CCK.Utils;
using Nox.Worlds;
using Nox.Worlds.Spawns;
using UnityEngine;
using Gizmos = Nox.CCK.Development.Gizmos;
using Logger = Nox.CCK.Utils.Logger;
using Random = UnityEngine.Random;

namespace Nox.CCK.Worlds.Spawns {
	[Gizmos("cck.worlds.spawns")]
	public class SpawnsWorldModule : MonoBehaviour, ISpawnModule, IGizmos, ICompilable {
		private IRuntimeWorld _runtime;

		public uint GetSpawnIndex()
			=> spawnIndex;

		public SpawnType GetSpawnType()
			=> spawnType;

		[SerializeReference]
		public ISpawn[] spawns = Array.Empty<ISpawn>();

		public SpawnType spawnType = SpawnType.Random;

		public uint spawnIndex;

		public ISpawn[] GetSpawns() {
			var hSpawns = new HashSet<ISpawn>();

			foreach (var spawn in spawns ?? Array.Empty<ISpawn>())
				hSpawns.Add(spawn);

			var groups = Array.Empty<ISpawnGroup>();

			foreach (var group in groups) {
				if (group == null || ReferenceEquals(group, this)) continue;
				foreach (var spawn in group.GetSpawns())
					hSpawns.Add(spawn);
			}

			return hSpawns.ToArray();
		}

		#if UNITY_EDITOR

		public int CompileOrder
			=> 8888;

		public void Compile() {
			spawns = EstimateSpawns().Values.ToArray();
		}

		private Dictionary<uint, ISpawn> EstimateSpawns() {
			var set = new HashSet<ISpawn>();

			foreach (var spawn in GetSpawns().Where(spawn => spawn != null))
				set.Add(spawn);

			var spawnsDict = new Dictionary<uint, ISpawn>();
			for (byte i = 0; i < set.Count; i++) {
				var estimatedId = i;
				while (spawnsDict.ContainsKey(estimatedId)) estimatedId++;
				spawnsDict.Add(estimatedId, set.ToArray()[i]);
			}

			return spawnsDict;
		}

		#endif

		public ISpawn ChoiceSpawn()
			=> (spawns?.Length ?? 0) switch {
				0 => new StructSpawn(transform),
				1 => spawns[0] ?? new StructSpawn(transform),
				_ => spawnType switch {
					SpawnType.Sequential => GetSequentialSpawn() ?? new StructSpawn(transform),
					SpawnType.Random     => spawns[Random.Range(0, spawns.Length)] ?? new StructSpawn(transform),
					SpawnType.Select     => spawns[spawnIndex % spawns.Length] ?? new StructSpawn(transform),
					SpawnType.Free       => GetFreeSpawn() ?? new StructSpawn(transform),
					_                    => spawns[0] ?? new StructSpawn(transform)
				}
			};

		private ISpawn GetSequentialSpawn() {
			var sp = spawns[spawnIndex % spawns.Length];
			spawnIndex++;
			return sp;
		}

		private ISpawn GetFreeSpawn() {
			var occupied = GetOccupied();

			if (occupied.Length == 0)
				return spawns[Random.Range(0, spawns.Length)];

			var mDis = 0f;
			var mSpw = spawns[0];

			foreach (var spawn in spawns) {
				var distance = occupied
					.Select(point => Vector3.Distance(spawn.Position, point))
					.Prepend(float.MaxValue)
					.Min();
				if (!(distance > mDis)) continue;
				mDis = distance;
				mSpw = spawn;
			}

			return mSpw;
		}

		private Vector3[] GetOccupied()
			=> Array.Empty<Vector3>();

		public static bool Check(IWorldDescriptor descriptor) {
			var modules = descriptor.GetModules<SpawnsWorldModule>();

			var module = modules.Length switch {
				1 => modules.FirstOrDefault(),
				0 => descriptor.Anchor.AddComponent<SpawnsWorldModule>(),
				_ => null
			};

			if (!module) {
				Logger.LogError("Verify that the World prefab has a valid SpawnsWorldModule component.");
				return false;
			}

			return true;
		}

		public async UniTask<bool> Setup(IRuntimeWorld runtime) {
			await UniTask.Yield();
			_runtime = runtime;
			return true;
		}

		#region Gizmos

		/// <summary>Radius of the disc drawn on the ground at each spawn, in metres.</summary>
		private const float GlyphRadius = 0.5f;

		/// <summary>Length of the vertical line drawn from each spawn, in metres.</summary>
		private const float GlyphHeight = 2f;

		/// <summary>Length of the shaft of the facing arrow, in metres.</summary>
		private const float ArrowLength = 0.75f;

		/// <summary>Length of the head of the facing arrow, in metres.</summary>
		private const float HeadLength = 0.3f;

		/// <summary>Full opening angle of the head of the facing arrow, in degrees.</summary>
		private const float HeadAngle = 30f;

		/// <summary>Colour of a spawn, unchanged by the selection.</summary>
		private static readonly Color SpawnColor = new(0f, 0.85f, 1f);

		/// <summary>Colour of the spawns while the module is selected.</summary>
		private static readonly Color SelectedColor = new(0.2f, 1f, 0.4f);

		/// <summary>Colour of the facing arrow.</summary>
		private static readonly Color ArrowColor = new(1f, 0.85f, 0.2f);

		/// <summary>
		/// Scene view glyphs. Public on purpose: <c>OnDrawGizmos</c> is a Unity message, the scene view calls it
		/// by itself — the <see cref="GizmosAttribute"/> only makes the glyphs pickable through
		/// <c>Nox.CCK.Editor.GizmosRenderer</c>.
		/// </summary>
		public void OnDrawGizmos() {
			var selected = gameObject.IsSelected();
			var color    = selected ? SelectedColor : SpawnColor;
			var entries  = spawns;

			// Without entries the module falls back on its own transform (see ChoiceSpawn).
			if (entries == null || entries.Length == 0) {
				DrawSpawnGlyph(transform.position, transform.rotation, color, -1, selected);
				return;
			}

			for (var i = 0; i < entries.Length; i++) {
				var spawn = entries[i];
				if (spawn == null) continue;
				DrawSpawnGlyph(spawn.Position, spawn.Rotation, color, i, selected);
			}
		}

		/// <summary>
		/// Points the renderer tests for picking: the glyphs are at the spawns, which are rarely around the pivot.
		/// </summary>
		public Vector3[] GetPickPoints() {
			var entries = spawns;
			if (entries == null || entries.Length == 0)
				return new[] { transform.position };

			var points = new List<Vector3>(entries.Length);
			foreach (var spawn in entries)
				if (spawn != null)
					points.Add(spawn.Position);

			return points.Count > 0 ? points.ToArray() : new[] { transform.position };
		}

		/// <summary>
		/// Draws one spawn: the ground disc (the room the player takes), the vertical line (its height) and the
		/// arrow (the direction the player will face). <paramref name="index"/> is negative for the fallback
		/// spawn of the module's transform.
		/// </summary>
		private static void DrawSpawnGlyph(Vector3 position, Quaternion rotation, Color color, int index, bool selected) {
			var up      = rotation * Vector3.up;      // ground normal of the spawn
			var forward = rotation * Vector3.forward; // where the player will face

			Gizmos.Color = color;

			// Ground disc: where the player appears.
			Gizmos.DrawWireDisc(position, up, GlyphRadius);

			// Vertical line: the spawn is on the ground, the player stands up to 2 m above it.
			Gizmos.DrawLine(position, position + up * GlyphHeight);

			// Facing arrow: the helper builds the head from the tip, so it points where the player will face.
			Gizmos.Color = ArrowColor;
			Gizmos.DrawArrow(position, forward, ArrowLength + HeadLength, HeadLength, HeadAngle);

			if (selected && index >= 0)
				Gizmos.DrawLabel(position + up * (GlyphHeight + 0.15f), $"#{index}");
		}

		#endregion
	}
}
