using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.CCK.Build;
using Nox.CCK.Development;
using Nox.Worlds;
using Nox.Worlds.Spawns;
using UnityEngine;
using Gizmos = Nox.CCK.Development.Gizmos;
using Logger = Nox.CCK.Utils.Logger;
using Random = UnityEngine.Random;

namespace Nox.CCK.Worlds.Spawns {
	public class SpawnsWorldModule : MonoBehaviour, ISpawnModule, IGizmos, ICompilable {
		private IRuntimeWorld _runtime;

		public uint GetSpawnIndex()
			=> spawnIndex;

		public SpawnType GetSpawnType()
			=> spawnType;

		[SerializeReference]
		public ISpawn[] spawns;

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


		public SpawnBehavior[] SpawnBehaviors {
			get
				=> spawns
						?.Select(e => e as SpawnBehavior)
						.Where(e => e)
						.ToArray()
					?? Array.Empty<SpawnBehavior>();
			set
				=> spawns = value
						?.Select(e => e as ISpawn)
						.ToArray()
					?? Array.Empty<ISpawn>();
		}

		#endif

		public ISpawn ChoiceSpawn()
			=> spawns.Length switch {
				0 => new StructSpawn(transform),
				1 => spawns[0],
				_ => spawnType switch {
					SpawnType.Sequential => GetSequentialSpawn(),
					SpawnType.Random     => spawns[Random.Range(0, spawns.Length)],
					SpawnType.Select     => spawns[spawnIndex % spawns.Length],
					SpawnType.Free       => GetFreeSpawn(),
					_                    => spawns[0]
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

		public void OnDrawGizmos() {
			if (!this || !enabled) return;
			Gizmos.color = Color.cyan;
			foreach (var spawn in spawns ?? Array.Empty<ISpawn>()) {
				if (spawn == null) continue;
				var pos     = spawn.Position;
				var rot     = spawn.Rotation;
				var forward = rot * Vector3.forward;
				Gizmos.DrawWireSphere(pos, 0.25f);
				Gizmos.DrawLine(pos, pos + forward * 0.5f);
			}
		}
	}
}
