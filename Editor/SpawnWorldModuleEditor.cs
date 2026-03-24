using Nox.CCK.Worlds;
using Nox.CCK.Worlds.Spawns;
using Nox.Worlds;
using Nox.Worlds.Spawns;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace Nox.Editor.Worlds.Spawns {
	[CustomEditor(typeof(SpawnsWorldModule))]
	public class SpawnWorldModuleEditor : UnityEditor.Editor {
		public override void OnInspectorGUI() {
			serializedObject.Update();

			// Affichage des propriétés par défaut
			EditorGUILayout.PropertyField(serializedObject.FindProperty("spawnType"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("spawnIndex"));

			EditorGUILayout.Space();

			// Section des spawns
			EditorGUILayout.LabelField("Spawns", EditorStyles.boldLabel);

			var spawnsProperty = serializedObject.FindProperty("spawns");

			// Affichage de la liste réordonnançable
			var reorderableList = new ReorderableList(serializedObject, spawnsProperty, true, true, true, true);
			reorderableList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Liste des Spawns");
			reorderableList.drawElementCallback = (rect, index, _, _) => {
				var element = spawnsProperty.GetArrayElementAtIndex(index);
				EditorGUI.ObjectField(
					new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight),
					element, typeof(SpawnBehavior)
				);
			};
			reorderableList.DoLayoutList();

			if (spawnsProperty.arraySize == 0)
				EditorGUILayout.HelpBox($"Aucun spawn configuré. Le spawn par défaut sera le {nameof(WorldDescriptor)}. Ajoutez des composants ISpawn à la liste.", MessageType.Warning);

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
