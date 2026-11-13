using Nox.CCK.Editor;
using Nox.CCK.Language;
using Nox.CCK.Worlds.ShaderVariants;
using Nox.Search;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nox.Editor.Worlds.ShaderVariants {
	/// <summary>
	/// Inspector of the shader variants module: the collections it warms up when the world loads, in order.
	/// </summary>
	public class ShaderVariantsWorldModuleEditor : LocalizedInspectorEditor<ShaderVariantsWorldModule> {
		/// <summary>Prefix of the language entries of this inspector (see the mod lang.asset).</summary>
		private const string LangPrefix = "worlds.modules.shader_variants.inspector";

		/// <summary>Icon of the component, from the assets of the <c>ui</c> mod.</summary>
		protected override ResourceTextureSource Icon
			=> "ui:icons/shader_variants.png";

		protected override string TitleKey
			=> LangPrefix + ".title";

		/// <summary>The list field, kept to localize its label on a language change.</summary>
		private PropertyField _variants;

		public override VisualElement CreateInspectorGUI() {
			var root = base.CreateInspectorGUI();
			if (Content == null)
				return root;

			// The GUI is rebuilt on every hierarchy change while the base keeps its root: never add twice.
			Content.Clear();

			var uxml = Resources.Load<VisualTreeAsset>(nameof(ShaderVariantsWorldModuleEditor));
			if (uxml == null) {
				Content.Add(new Label($"The {nameof(ShaderVariantsWorldModuleEditor)} uxml template is not found in Resources."));
				return root;
			}

			var ui = uxml.CloneTree();
			Content.Add(ui);

			// Fields are bound by name: an InspectorElement(serializedObject) would run this very editor again.
			_variants = BindField(ui, nameof(ShaderVariantsWorldModule.variants));

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

			if (_variants != null)
				_variants.label = LanguageManager.Get(LangPrefix + ".collections");
		}
	}
}
