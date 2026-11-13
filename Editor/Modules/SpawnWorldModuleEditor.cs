using System;
using System.Collections.Generic;
using System.Linq;
using Nox.CCK.Editor;
using Nox.CCK.Language;
using Nox.CCK.Worlds.Spawns;
using Nox.Search;
using Nox.Worlds.Spawns;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Nox.Editor.Worlds.Spawns {
	/// <summary>
	/// Inspector of the spawns module: how the module picks a spawn, and the entries it picks from.
	/// <para>
	/// <c>spawns</c> is a <c>[SerializeReference]</c> array, and an entry is unusable until it has a type:
	/// Unity's own array field adds an entry with no type, which nothing can edit. The list is therefore a
	/// <see cref="ChoiceListView"/>, whose « + » offers the types (<c>TransformSpawn</c>, <c>StructSpawn</c>,
	/// or any serializable <c>ISpawn</c> of the project) and then appends the entry.
	/// </para>
	/// </summary>
	public class SpawnWorldModuleEditor : LocalizedInspectorEditor<SpawnsWorldModule> {
		/// <summary>Prefix of the language entries of this inspector (see the mod lang.asset).</summary>
		private const string LangPrefix = "worlds.modules.spawns.inspector";

		/// <summary>The fields, with the language entry naming each of them.</summary>
		private static readonly (string property, string entry)[] Fields = {
			(nameof(SpawnsWorldModule.spawnType), "spawn_type"),
			(nameof(SpawnsWorldModule.spawnIndex), "spawn_index")
		};

		/// <summary>Icon of the component: the « my_location » glyph rendered in the assets of the <c>ui</c> mod.</summary>
		protected override ResourceTextureSource Icon
			=> "ui:icons/spawns.png";

		protected override string TitleKey
			=> LangPrefix + ".title";

		/// <summary>The bound fields, kept to localize their labels on a language change.</summary>
		private readonly Dictionary<string, PropertyField> _bound = new();

		/// <summary>The entries of the module: its « + » offers the spawn types, its « − » removes the selected one.</summary>
		private ChoiceListView _entries;

		/// <summary>The warning shown while no spawn is configured.</summary>
		private LocalizedNotificationElement _none;

		public override VisualElement CreateInspectorGUI() {
			var root = base.CreateInspectorGUI();
			if (Content == null)
				return root;

			// The GUI is rebuilt on every hierarchy change while the base keeps its root: never add twice.
			Content.Clear();

			var uxml = Resources.Load<VisualTreeAsset>(nameof(SpawnWorldModuleEditor));
			if (uxml == null) {
				Content.Add(new Label($"The {nameof(SpawnWorldModuleEditor)} uxml template is not found in Resources."));
				return root;
			}

			var ui = uxml.CloneTree();
			Content.Add(ui);

			// Fields are bound by name: an InspectorElement(serializedObject) would run this very editor again.
			_bound.Clear();
			foreach (var (property, entry) in Fields)
				_bound[entry] = BindField(ui, property);

			_none = ui.Q<LocalizedNotificationElement>("none");

			if (ui.Q<VisualElement>("list") is { } host) {
				_entries = CreateEntries();
				host.Add(_entries);
				FillChoices();
			}

			RebuildEntries();

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
		/// The list of entries: one <see cref="PropertyField"/> per entry, and the footer buttons that add (through
		/// the type menu) and remove.
		/// </summary>
		private ChoiceListView CreateEntries() {
			var list = new ChoiceListView {
				name           = "spawns",
				headerTitle    = LanguageManager.Get(LangPrefix + ".spawns"),
				RemoveSelected = RemoveEntries,
				MoveRequested  = MoveEntry
			};

			list.makeItem   = () => new PropertyField { name = "fields" };
			list.unbindItem = (element, _) => (element as PropertyField)?.Unbind();
			list.bindItem = (element, index) => {
				if (element is PropertyField field && index < list.itemsSource.Count && list.itemsSource[index] is SerializedProperty entry)
					field.BindProperty(entry);
			};

			return list;
		}

		/// <summary>Gives the « + » popup one choice per serializable spawn type of the project.</summary>
		private void FillChoices() {
			if (_entries == null)
				return;

			_entries.ClearChoices();

			foreach (var type in SpawnTypes()) {
				var selected = type;
				_entries.AddChoice(selected.Name, () => AddEntry(selected));
			}
		}

		/// <summary>
		/// Localizes what the shared localization cannot: the label of a <see cref="PropertyField"/> is the name
		/// of the field, so it is set here — on attach, and again on a language change.
		/// </summary>
		protected override void LocalizeTexts() {
			base.LocalizeTexts();

			foreach (var (_, entry) in Fields)
				if (_bound.TryGetValue(entry, out var field) && field != null)
					field.label = LanguageManager.Get(LangPrefix + "." + entry);

			if (_entries != null)
				_entries.headerTitle = LanguageManager.Get(LangPrefix + ".spawns");
		}

		protected override void OnAttachToPanel(AttachToPanelEvent evt) {
			base.OnAttachToPanel(evt);

			// An undo changes the array behind the list: redraw it, the fields of a removed entry would stay.
			Undo.undoRedoPerformed -= OnUndoRedo;
			Undo.undoRedoPerformed += OnUndoRedo;
		}

		protected override void OnDetachFromPanel(DetachFromPanelEvent evt) {
			Undo.undoRedoPerformed -= OnUndoRedo;

			base.OnDetachFromPanel(evt);
		}

		private void OnUndoRedo()
			=> RebuildEntries();

		/// <summary>
		/// Types an entry can have: serializable <c>ISpawn</c> classes. Components are left out — a managed
		/// reference serializes the fields of the object, not a reference, so a scene object has nothing to do
		/// in there.
		/// </summary>
		private static Type[] SpawnTypes()
			=> TypeCache.GetTypesDerivedFrom<ISpawn>()
				.Where(type => type.IsClass && !type.IsAbstract)
				.Where(type => Attribute.IsDefined(type, typeof(SerializableAttribute)))
				.Where(type => !typeof(Object).IsAssignableFrom(type))
				.OrderBy(type => type.Name)
				.ToArray();

		/// <summary>Appends an entry of <paramref name="type"/>, then redraws the list so it can be edited.</summary>
		private void AddEntry(Type type) {
			serializedObject.Update();

			var property = serializedObject.FindProperty(nameof(SpawnsWorldModule.spawns));
			var index    = property.arraySize;

			property.arraySize = index + 1;
			property.GetArrayElementAtIndex(index).managedReferenceValue = Activator.CreateInstance(type);

			serializedObject.ApplyModifiedProperties();
			RebuildEntries();
		}

		/// <summary>
		/// Removes the entries at <paramref name="indices"/> (highest first, as the list hands them over), then
		/// redraws the list: the fields of the entries after them were bound to properties that no longer exist.
		/// </summary>
		private void RemoveEntries(IEnumerable<int> indices) {
			var property = serializedObject.FindProperty(nameof(SpawnsWorldModule.spawns));
			if (property == null)
				return;

			serializedObject.Update();

			foreach (var index in indices)
				if (index >= 0 && index < property.arraySize)
					property.DeleteArrayElementAtIndex(index);

			serializedObject.ApplyModifiedProperties();
			RebuildEntries();
		}

		/// <summary>Moves an entry, the order being what <c>SpawnType.Sequential</c> / <c>Select</c> walks.</summary>
		private void MoveEntry(int from, int to) {
			var property = serializedObject.FindProperty(nameof(SpawnsWorldModule.spawns));
			if (property == null || from < 0 || to < 0 || from >= property.arraySize || to >= property.arraySize)
				return;

			serializedObject.Update();
			property.MoveArrayElement(from, to);
			serializedObject.ApplyModifiedProperties();

			RebuildEntries();
		}

		/// <summary>Gives the list one <see cref="SerializedProperty"/> per entry of the module.</summary>
		private void RebuildEntries() {
			if (_entries == null)
				return;

			serializedObject.Update();

			var property = serializedObject.FindProperty(nameof(SpawnsWorldModule.spawns));
			var entries  = new List<SerializedProperty>(property.arraySize);
			for (var i = 0; i < property.arraySize; i++)
				entries.Add(property.GetArrayElementAtIndex(i));

			_entries.itemsSource = entries;
			_entries.Rebuild();

			RefreshNone();
		}

		/// <summary>
		/// Shows the warning while the module has no spawn: it then spawns on its own transform, which the world
		/// check accepts but which is rarely what the author means to test.
		/// </summary>
		private void RefreshNone() {
			if (_none == null)
				return;

			_none.EnableInClassList("hidden", (Target.spawns?.Length ?? 0) > 0);
		}
	}
}
