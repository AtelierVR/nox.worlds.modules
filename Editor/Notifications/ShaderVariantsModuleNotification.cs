using System.Linq;
using Nox.Assets.Editor;
using Nox.CCK.Worlds;
using Nox.CCK.Worlds.ShaderVariants;
using Nox.Worlds.Runtime.Editor;
using UnityEditor;

namespace Nox.Editor.Worlds.Modules {
	/// <summary>
	/// State of the <see cref="ShaderVariantsWorldModule"/> of the selected world: the module itself (added
	/// by its <c>Check</c>, refused when there are several) and its collections — the module only exists to
	/// warm them up, so an empty list means it does nothing.
	/// </summary>
	public static class ShaderVariantsModuleNotification {
		private const string ModuleUid = "worlds.modules.shader_variants.module";
		private const string EmptyUid  = "worlds.modules.shader_variants.none";
		private const string Module    = "Shader variants";

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
				WorldModuleNotification.Hide(EmptyUid);
				return;
			}

			var modules = WorldModuleNotification.Modules<ShaderVariantsWorldModule>(world);

			switch (modules.Length) {
				case 0:
					WorldModuleNotification.ShowMissing<ShaderVariantsWorldModule>(
						ModuleUid,
						world,
						Module,
						"worlds.modules.shader_variants.missing",
						() => Update(world)
					);
					WorldModuleNotification.Hide(EmptyUid);
					return;
				case 1:
					WorldModuleNotification.Hide(ModuleUid);
					break;
				default:
					WorldModuleNotification.ShowDuplicated(ModuleUid, Module, modules.Length, modules[0]);
					break;
			}

			if (modules.Any(module => module.variants is { Length: > 0 })) {
				WorldModuleNotification.Hide(EmptyUid);
				return;
			}

			WorldModuleNotification.Show(
				EmptyUid,
				AssetNotificationType.Info,
				"worlds.modules.shader_variants.none",
				null,
				WorldModuleNotification.Select(modules[0])
			);
		}
	}
}
