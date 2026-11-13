using Nox.CCK.Editor;
using Nox.CCK.Language;
using Nox.CCK.Worlds.Scenes;
using Nox.Search;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nox.Editor.Worlds.Scenes {
	/// <summary>
	/// Inspector of the scenes module: it shows the list of scene assets the author edits.
	/// <para>
	/// The compiled <c>scenes</c> paths are left out on purpose — the module fills them when the world is
	/// compiled, so showing them would only offer a field that is overwritten.
	/// </para>
	/// </summary>
	public class ScenesWorldModuleEditor : LocalizedInspectorEditor<ScenesWorldModule> {
		/// <summary>Prefix of the language entries of this inspector (see the mod lang.asset).</summary>
		private const string LangPrefix = "worlds.modules.scenes.inspector";

		/// <summary>Icon of the component, from the assets of the <c>ui</c> mod.</summary>
		protected override ResourceTextureSource Icon
			=> "ui:icons/scenes.png";

		protected override string TitleKey
			=> LangPrefix + ".title";

		/// <summary>The list field, kept to localize its label on a language change.</summary>
		private PropertyField _scenes;

		public override VisualElement CreateInspectorGUI() {
			var root = base.CreateInspectorGUI();
			if (Content == null)
				return root;

			// The GUI is rebuilt on every hierarchy change while the base keeps its root: never add twice.
			Content.Clear();

			var uxml = Resources.Load<VisualTreeAsset>(nameof(ScenesWorldModuleEditor));
			if (uxml == null) {
				Content.Add(new Label($"The {nameof(ScenesWorldModuleEditor)} uxml template is not found in Resources."));
				return root;
			}

			var ui = uxml.CloneTree();
			Content.Add(ui);

			// Fields are bound by name: an InspectorElement(serializedObject) would run this very editor again.
			_scenes = BindField(ui, nameof(ScenesWorldModule.sceneAssets));

			return root;
		}

		private PropertyField BindField(VisualElement root, string propertyName) {
			var field    = root.Q<PropertyField>(propertyName);
			var property = serializedObject.FindProperty(propertyName);
			if (field != null && property != null)
				field.BindProperty(property);

			return field;
		}

		/// <summary>
		/// Localizes what the shared localization cannot: the label of a <see cref="PropertyField"/> is the name
		/// of the field, so it is set here — on attach, and again on a language change.
		/// </summary>
		protected override void LocalizeTexts() {
			base.LocalizeTexts();

			if (_scenes != null)
				_scenes.label = LanguageManager.Get(LangPrefix + ".scene_assets");
		}
	}
}
