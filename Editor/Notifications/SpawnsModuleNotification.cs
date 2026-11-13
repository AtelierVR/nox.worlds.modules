using System;
using System.Linq;
using Nox.Assets.Editor;
using Nox.CCK.Worlds;
using Nox.CCK.Worlds.FellInVoid;
using Nox.CCK.Worlds.Spawns;
using Nox.Worlds.Runtime.Editor;
using Nox.Worlds.Spawns;
using UnityEditor;
using UnityEngine;

namespace Nox.Editor.Worlds.Modules {
	/// <summary>
	/// State of the spawns of the selected world: the module itself (added by its <c>Check</c>, refused when
	/// there are several), the spawns it declares, and where the players actually appear.
	/// <para>
	/// The position check never assumes a spawn is configured: <see cref="SpawnsWorldModule.ChoiceSpawn"/>
	/// falls back to the transform of the module when it has no spawn, and <c>Check</c> places that module
	/// on the WorldDescriptor — so the descriptor itself is what is below the fall threshold when the list
	/// is empty. The threshold is the one of the <see cref="FellInVoidWorldModule"/>, or the default one it
	/// is created with.
	/// </para>
	/// </summary>
	public static class SpawnsModuleNotification {
		private const string ModuleUid         = "worlds.modules.spawn.module";
		private const string BelowThresholdUid = "worlds.modules.spawn.below_threshold";
		private const string NoSpawnUid        = "worlds.modules.spawn.none";
		private const string Module            = "Spawns";

		/// <summary>Guards the burst of change events a drag or a multi-edit produces.</summary>
		private static bool _refreshQueued;

		[InitializeOnLoadMethod]
		private static void Initialize() {
			WorldDescriptorHelper.OnWorldSelected.AddListener(Update);
			EditorApplication.hierarchyChanged += OnHierarchyChanged;
			ObjectChangeEvents.changesPublished += OnChangesPublished;
			Update(WorldDescriptorHelper.CurrentWorld);
		}

		private static void OnHierarchyChanged()
			=> Update(WorldDescriptorHelper.CurrentWorld);

		/// <summary>
		/// Moving the descriptor or editing the spawns does not raise <c>hierarchyChanged</c>, yet both decide
		/// whether a spawn is under the fall threshold. The events are coalesced into one update.
		/// </summary>
		private static void OnChangesPublished(ref ObjectChangeEventStream stream) {
			for (var i = 0; i < stream.length; i++) {
				if (stream.GetEventType(i) is not (ObjectChangeKind.ChangeGameObjectOrComponentProperties
					or ObjectChangeKind.ChangeGameObjectStructure))
					continue;

				QueueRefresh();
				return;
			}
		}

		private static void QueueRefresh() {
			if (_refreshQueued)
				return;

			_refreshQueued = true;
			EditorApplication.delayCall += () => {
				_refreshQueued = false;
				Update(WorldDescriptorHelper.CurrentWorld);
			};
		}

		private static void Update(WorldDescriptor world) {
			if (!world) {
				WorldModuleNotification.Hide(ModuleUid);
				WorldModuleNotification.Hide(NoSpawnUid);
				WorldModuleNotification.Hide(BelowThresholdUid);
				return;
			}

			var modules = WorldModuleNotification.Modules<SpawnsWorldModule>(world);

			switch (modules.Length) {
				case 0:
					WorldModuleNotification.ShowMissing<SpawnsWorldModule>(
						ModuleUid,
						world,
						Module,
						"worlds.modules.spawn.missing",
						() => Update(world)
					);
					break;
				case 1:
					WorldModuleNotification.Hide(ModuleUid);
					break;
				default:
					WorldModuleNotification.ShowDuplicated(ModuleUid, Module, modules.Length, modules[0]);
					break;
			}

			var spawns = modules
				.SelectMany(module => module.spawns ?? Array.Empty<ISpawn>())
				.Where(spawn => spawn != null)
				.ToArray();

			// « Nothing selected » is about the module the action points at: the descriptor when there is none.
			Component target = modules.Length > 0 ? modules[0] : world;

			if (spawns.Length > 0)
				WorldModuleNotification.Hide(NoSpawnUid);
			else
				WorldModuleNotification.Show(
					NoSpawnUid,
					AssetNotificationType.Info,
					"worlds.modules.spawn.none",
					null,
					WorldModuleNotification.Select(target)
				);

			// Where a player can appear: the declared spawns, or the fallback the module uses without any —
			// its own transform, i.e. the WorldDescriptor when the module does not exist yet.
			var positions = spawns.Length > 0
				? spawns.Select(spawn => spawn.Position).ToArray()
				: modules.Select(module => module.transform.position).DefaultIfEmpty(world.transform.position).ToArray();

			var fall      = WorldModuleNotification.Modules<FellInVoidWorldModule>(world).FirstOrDefault();
			var threshold = fall ? fall.fallThreshold : FellInVoidWorldModule.DefaultFallThreshold;

			var below = positions.Count(position => position.y < threshold);
			if (below == 0) {
				WorldModuleNotification.Hide(BelowThresholdUid);
				return;
			}

			WorldModuleNotification.Show(
				BelowThresholdUid,
				AssetNotificationType.Error,
				"worlds.modules.spawn.below_threshold",
				new object[] { below, threshold },
				WorldModuleNotification.Select(target)
			);
		}
	}
}
