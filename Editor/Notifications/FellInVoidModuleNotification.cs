using Nox.CCK.Worlds;
using Nox.CCK.Worlds.FellInVoid;
using Nox.Worlds.Runtime.Editor;
using UnityEditor;

namespace Nox.Editor.Worlds.Modules {
	/// <summary>
	/// State of the <see cref="FellInVoidWorldModule"/> of the selected world: <c>Check</c> adds it when
	/// there is none (information) and returns <c>false</c> when there are several, which blocks the build.
	/// </summary>
	public static class FellInVoidModuleNotification {
		private const string Uid    = "worlds.modules.fell_in_void.module";
		private const string Module = "FellInVoid";

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
				WorldModuleNotification.Hide(Uid);
				return;
			}

			var modules = WorldModuleNotification.Modules<FellInVoidWorldModule>(world);

			switch (modules.Length) {
				case 1:
					WorldModuleNotification.Hide(Uid);
					break;
				case 0:
					WorldModuleNotification.ShowMissing<FellInVoidWorldModule>(
						Uid,
						world,
						Module,
						"worlds.modules.fell_in_void.missing",
						() => Update(world),
						FellInVoidWorldModule.DefaultFallThreshold
					);
					break;
				default:
					WorldModuleNotification.ShowDuplicated(Uid, Module, modules.Length, modules[0]);
					break;
			}
		}
	}
}
