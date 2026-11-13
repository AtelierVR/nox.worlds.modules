using System.Linq;
using Nox.Assets.Editor;
using Nox.CCK.Worlds;
using Nox.CCK.Worlds.Scenes;
using Nox.Worlds.Runtime.Editor;
using UnityEditor;

namespace Nox.Editor.Worlds.Modules {
	/// <summary>
	/// State of the <see cref="ScenesWorldModule"/> of the selected world: the module itself (added by its
	/// <c>Check</c>, refused when there are several) and the scene assets removed from the project, which
	/// leave a hole the build compiles as a missing scene.
	/// </summary>
	public static class ScenesModuleNotification {
		private const string ModuleUid  = "worlds.modules.scenes.module";
		private const string MissingUid = "worlds.modules.scenes.missing";
		private const string Module     = "Scenes";

		[InitializeOnLoadMethod]
		private static void Initialize() {
			WorldDescriptorHelper.OnWorldSelected.AddListener(Update);
			EditorApplication.hierarchyChanged += OnHierarchyChanged;
			Update(WorldDescriptorHelper.CurrentWorld);
		}

		private static void OnHierarchyChanged()
			=> Update(WorldDescriptorHelper.CurrentWorld);

		private static void Update(WorldDescriptor world) {
			// No active descriptor: there is no world to report a missing module on.
			if (!world) {
				WorldModuleNotification.Hide(ModuleUid);
				WorldModuleNotification.Hide(MissingUid);
				return;
			}

			var modules = WorldModuleNotification.Modules<ScenesWorldModule>(world);

			switch (modules.Length) {
				case 0:
					WorldModuleNotification.ShowMissing<ScenesWorldModule>(
						ModuleUid,
						world,
						Module,
						"worlds.modules.scenes.module_missing",
						() => Update(world)
					);
					WorldModuleNotification.Hide(MissingUid);
					return;
				case 1:
					WorldModuleNotification.Hide(ModuleUid);
					break;
				default:
					WorldModuleNotification.ShowDuplicated(ModuleUid, Module, modules.Length, modules[0]);
					break;
			}

			var missing = modules.Sum(module => module.GetSceneAssets().Count(scene => !scene));
			if (missing == 0) {
				WorldModuleNotification.Hide(MissingUid);
				return;
			}

			WorldModuleNotification.Show(
				MissingUid,
				AssetNotificationType.Warning,
				"worlds.modules.scenes.missing",
				new object[] { missing },
				WorldModuleNotification.Select(modules.FirstOrDefault(module => module.GetSceneAssets().Any(scene => !scene)))
			);
		}
	}
}
