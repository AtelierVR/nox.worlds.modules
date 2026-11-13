using Nox.CCK.Editor;
using Nox.CCK.Worlds.CustomMenu;
using Nox.Search;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nox.Editor.Worlds.CustomMenu {
	public class CustomMenuModuleEditor : LocalizedInspectorEditor<CustomMenuModule> {

		/// <summary>Prefix of the language entries of this inspector (see the mod lang.asset).</summary>
		private const string LangPrefix = "worlds.modules.custom_menu.inspector";

		protected override string TitleKey
			=> LangPrefix + ".title";

		/// <summary>Icon of the component, from the assets of the <c>ui</c> mod.</summary>
		protected override ResourceTextureSource Icon
			=> "ui:icons/custom_menu.png";

		public override VisualElement CreateInspectorGUI() {
            var root = base.CreateInspectorGUI();
            if (Content == null)
                return root;

        	// The GUI is rebuilt on every hierarchy change while the base keeps its root: never add twice.
            Content.Clear();

			var uxml = Resources.Load<VisualTreeAsset>(nameof(CustomMenuModuleEditor));
			if (uxml == null) {
				Content.Add(new Label($"The {nameof(CustomMenuModuleEditor)} uxml template is not found in Resources."));
				return root;
			}

			var ui = uxml.CloneTree();
			Content.Add(ui);

			// NOT InspectorElement(serializedObject): it creates a nested inspector that runs this very
			// CustomEditor again (stack overflow). Each field is bound by name instead.
			BindField(ui, nameof(CustomMenuModule.prefab));
			BindField(ui, nameof(CustomMenuModule.label));
			BindField(ui, nameof(CustomMenuModule.icon));
			BindField(ui, nameof(CustomMenuModule.onMenuShown));
			BindField(ui, nameof(CustomMenuModule.onMenuHidden));

			ShowWarning(ui);
			return root;
		}

		private void BindField(VisualElement root, string propertyName) {
			var field    = root.Q<PropertyField>(propertyName);
			var property = serializedObject.FindProperty(propertyName);
			if (field != null && property != null)
				field.BindProperty(property);
		}

		private void ShowWarning(VisualElement root) {
			var prefab = Target.prefab;
			if (prefab) {
				if (!prefab.TryGetComponent<RectTransform>(out _))
					ShowWarning(root, LangPrefix + ".warning_not_ugui");
				return;
			}

			ShowWarning(root, LangPrefix + ".warning_no_prefab");
		}

		/// <summary>
		/// Shows the notification with the language entry <paramref name="key"/>: the label is a
		/// <see cref="LocalizedNotificationElement"/>, so the text follows the language changes (see
		/// <see cref="LocalizedInspectorEditor{T}"/>).
		/// </summary>
		private static void ShowWarning(VisualElement root, string key) {
			if (root.Q<LocalizedNotificationElement>("warning") is not { } warning)
				return;

			warning.LangKey = key;
			warning.RemoveFromClassList("hidden");
		}
	}
}
