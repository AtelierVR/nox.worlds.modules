using System;
using System.Linq;
using Nox.CCK.Worlds.Spawns;
using Nox.Worlds.Spawns;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using Object = UnityEngine.Object;

namespace Nox.Editor.Worlds.Spawns {
	[CustomEditor(typeof(SpawnsWorldModule))]
	public class SpawnWorldModuleEditor : UnityEditor.Editor {
		/// <summary>
		/// Types proposables pour une entrée de <see cref="SpawnsWorldModule.spawns"/> : des classes
		/// <c>[Serializable]</c> qui implémentent <see cref="ISpawn"/>. Les composants en sont exclus :
		/// une référence managée sérialise les champs de l'objet, pas une référence d'objet, donc un objet
		/// de scène n'a rien à y faire.
		/// </summary>
		private static Type[] SpawnTypes()
			=> TypeCache.GetTypesDerivedFrom<ISpawn>()
				.Where(type => type.IsClass && !type.IsAbstract)
				.Where(type => Attribute.IsDefined(type, typeof(SerializableAttribute)))
				.Where(type => !typeof(Object).IsAssignableFrom(type))
				.OrderBy(type => type.Name)
				.ToArray();

		/// <summary>
		/// Une entrée de tableau <c>[SerializeReference]</c> tant qu'aucun type ne lui a été donné : Unity
		/// n'en dessine alors rien du tout et elle reste inutilisable.
		/// </summary>
		private static bool IsEmpty(SerializedProperty element)
			=> element.propertyType == SerializedPropertyType.ManagedReference
				&& string.IsNullOrEmpty(element.managedReferenceFullTypename);

		private static void ShowTypeMenu(Rect rect, Action<Type> onSelected) {
			var menu  = new GenericMenu();
			var types = SpawnTypes();

			if (types.Length == 0)
				menu.AddDisabledItem(new GUIContent("Aucun type de spawn sérialisable"));
			else
				foreach (var type in types) {
					var selected = type;
					menu.AddItem(new GUIContent(selected.Name), false, () => onSelected(selected));
				}

			menu.DropDown(rect);
		}

		/// <summary>
		/// Dessine les champs d'une entrée à plat : Unity ajoute sinon un en-tête repliable autour du type,
		/// qui prenait une ligne pour rien.
		/// </summary>
		private static void DrawFields(Rect rect, SerializedProperty element) {
			var child = element.Copy();
			var end   = child.GetEndProperty();
			var y     = rect.y;
			var enter = true;

			while (child.NextVisible(enter) && !SerializedProperty.EqualContents(child, end)) {
				enter = false;

				// Sans descente dans l'entrée (type non résolu), l'itérateur passerait à l'entrée suivante
				if (child.depth <= element.depth)
					break;

				var height = EditorGUI.GetPropertyHeight(child, true);
				EditorGUI.PropertyField(new Rect(rect.x, y, rect.width, height), child, true);
				y += height + EditorGUIUtility.standardVerticalSpacing;
			}
		}

		/// <summary>Hauteur cumulée de ces champs, espaces entre eux compris.</summary>
		private static float FieldsHeight(SerializedProperty element) {
			var child = element.Copy();
			var end   = child.GetEndProperty();
			var total = 0f;
			var enter = true;
			var first = true;

			while (child.NextVisible(enter) && !SerializedProperty.EqualContents(child, end)) {
				enter = false;

				if (child.depth <= element.depth)
					break;

				if (!first)
					total += EditorGUIUtility.standardVerticalSpacing;

				first = false;
				total += EditorGUI.GetPropertyHeight(child, true);
			}

			return Mathf.Max(total, EditorGUIUtility.singleLineHeight);
		}

		public override void OnInspectorGUI() {
			serializedObject.Update();

			// Affichage des propriétés par défaut
			EditorGUILayout.PropertyField(serializedObject.FindProperty("spawnType"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("spawnIndex"));

			EditorGUILayout.Space();

			// Section des spawns
			EditorGUILayout.LabelField("Spawns", EditorStyles.boldLabel);

			var spawnsProperty  = serializedObject.FindProperty("spawns");
			var reorderableList = new ReorderableList(serializedObject, spawnsProperty, true, true, true, true);

			// `spawns` est un tableau de références managées : une entrée n'a de type qu'une fois choisi.
			// ReorderableList ne propose pas ce choix de lui-même, donc « + » n'ajoutait qu'une ligne vide
			// et impossible à renseigner.
			reorderableList.onAddDropdownCallback = (buttonRect, list) => ShowTypeMenu(
				buttonRect,
				type => {
					var index = spawnsProperty.arraySize;
					spawnsProperty.arraySize = index + 1;
					spawnsProperty.GetArrayElementAtIndex(index).managedReferenceValue = Activator.CreateInstance(type);
					list.index = index;
					serializedObject.ApplyModifiedProperties();
				}
			);

			reorderableList.elementHeightCallback = index => {
				var element = spawnsProperty.GetArrayElementAtIndex(index);
				var height  = IsEmpty(element) ? EditorGUIUtility.singleLineHeight : FieldsHeight(element);
				return height + EditorGUIUtility.standardVerticalSpacing;
			};

			reorderableList.drawElementCallback = (rect, index, _, _) => {
				var element = spawnsProperty.GetArrayElementAtIndex(index);

				if (IsEmpty(element)) {
					rect.height = EditorGUIUtility.singleLineHeight;

					if (EditorGUI.DropdownButton(rect, new GUIContent("Choisir un type de spawn…"), FocusType.Keyboard))
						ShowTypeMenu(
							rect,
							type => {
								element.managedReferenceValue = Activator.CreateInstance(type);
								serializedObject.ApplyModifiedProperties();
							}
						);

					return;
				}

				DrawFields(rect, element);
			};

			reorderableList.DoLayoutList();

			if (spawnsProperty.arraySize == 0)
				EditorGUILayout.HelpBox(
					$"Aucun spawn configuré. Le spawn par défaut sera la position de ce module. "
					+ $"Ajoutez un {nameof(TransformSpawn)} (position d'un objet de la scène) ou un {nameof(StructSpawn)} (coordonnées).",
					MessageType.Warning
				);

			EditorGUILayout.Space();

			// Section récapitulatif (uniquement en mode play)
			if (Application.isPlaying) {
				EditorGUILayout.Space();
				EditorGUILayout.LabelField("Informations Runtime", EditorStyles.boldLabel);

				var spawnTypeProperty  = serializedObject.FindProperty("spawnType");
				var spawnIndexProperty = serializedObject.FindProperty("spawnIndex");

				EditorGUILayout.LabelField($"Spawns configurés: {spawnsProperty.arraySize}", EditorStyles.miniLabel);
				EditorGUILayout.LabelField($"Type de spawn: {spawnTypeProperty.enumDisplayNames[spawnTypeProperty.enumValueIndex]}", EditorStyles.miniLabel);
				EditorGUILayout.LabelField($"Index de spawn: {spawnIndexProperty.uintValue}", EditorStyles.miniLabel);
			}

			serializedObject.ApplyModifiedProperties();
		}
	}
}
