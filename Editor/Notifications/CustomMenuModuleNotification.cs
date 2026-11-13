using System;
using System.Linq;
using Nox.Assets.Editor;
using Nox.CCK.Worlds;
using Nox.CCK.Worlds.CustomMenu;
using Nox.Worlds.Runtime.Editor;
using UnityEditor;
using UnityEngine;

namespace Nox.Editor.Worlds.Modules {
	/// <summary>
	/// Custom page of the player menu: without a prefab the module adds no page (info), and a prefab that
	/// is not a uGUI one leaves the page empty (warning) — the menu displays it inside its own uGUI tree.
	/// <para>
	/// <see cref="CustomMenuModule"/> is not an <c>IWorldModule</c> (the client resolves it from the world
	/// anchor), so it is searched in the anchor subtree instead of the module list.
	/// </para>
	/// </summary>
	public static class CustomMenuModuleNotification {
		private const string NotUguiUid  = "worlds.modules.custom_menu.not_ugui";
		private const string NoPrefabUid = "worlds.modules.custom_menu.no_prefab";

		[InitializeOnLoadMethod]
		private static void Initialize() {
			WorldDescriptorHelper.OnWorldSelected.AddListener(Update);
			EditorApplication.hierarchyChanged += OnHierarchyChanged;
			Update(WorldDescriptorHelper.CurrentWorld);
		}

		private static void OnHierarchyChanged()
			=> Update(WorldDescriptorHelper.CurrentWorld);

		private static void Update(WorldDescriptor world) {
			var modules = Find(world);
			if (modules.Length == 0) {
				WorldModuleNotification.Hide(NotUguiUid);
				WorldModuleNotification.Hide(NoPrefabUid);
				return;
			}

			var notUgui = modules.Where(IsNotUgui).ToArray();
			if (notUgui.Length > 0)
				WorldModuleNotification.Show(
					NotUguiUid,
					AssetNotificationType.Warning,
					"worlds.modules.custom_menu.not_ugui",
					new object[] { notUgui.Length },
					WorldModuleNotification.Select(notUgui[0])
				);
			else
				WorldModuleNotification.Hide(NotUguiUid);

			if (modules.All(module => !module.prefab))
				WorldModuleNotification.Show(
					NoPrefabUid,
					AssetNotificationType.Info,
					"worlds.modules.custom_menu.no_prefab",
					null,
					WorldModuleNotification.Select(modules[0])
				);
			else
				WorldModuleNotification.Hide(NoPrefabUid);
		}

		private static CustomMenuModule[] Find(WorldDescriptor world)
			=> world && world.Anchor
				? world.Anchor.GetComponentsInChildren<CustomMenuModule>(true)
				: Array.Empty<CustomMenuModule>();

		/// <summary>A prefab without a <see cref="RectTransform"/> cannot be laid out inside the menu.</summary>
		private static bool IsNotUgui(CustomMenuModule module)
			=> module && module.prefab && !module.prefab.TryGetComponent<RectTransform>(out _);
	}
}
