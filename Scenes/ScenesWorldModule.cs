using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.CCK.Build;
using Nox.Worlds;
using Nox.Worlds.Scenes;
using UnityEditor;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.CCK.Worlds.Scenes {
	public class ScenesWorldModule : MonoBehaviour, IScenesModule, ICompilable {
		public string[] scenes;

		#if UNITY_EDITOR
		public List<SceneAsset> sceneAssets;

		public string[] GetScenes()
			=> GetSceneAssets()
				.ToList()
				.ConvertAll(AssetDatabase.GetAssetPath)
				.ToArray();

		public int CompileOrder
			=> 9999;

		public void Compile() {
			sceneAssets = this.EstimateScenes().Values.ToList();
			sceneAssets.RemoveAt(0);
			scenes = GetScenes().ToArray();
		}

		private Dictionary<byte, SceneAsset> EstimateScenes() {
			var set = new HashSet<SceneAsset> { AssetDatabase.LoadAssetAtPath<SceneAsset>(gameObject.scene.path) };
			foreach (var scenePath in GetSceneAssets().Where(scenePath => scenePath))
				set.Add(scenePath);

			var scenesDict = new Dictionary<byte, SceneAsset>();
			for (byte i = 0; i < set.Count; i++) {
				var estimatedId = i;
				while (scenesDict.ContainsKey(estimatedId)) estimatedId++;
				scenesDict.Add(estimatedId, set.ToArray()[i]);
			}

			return scenesDict;
		}

		public SceneAsset[] GetSceneAssets()
			=> sceneAssets?.ToArray()
				?? Array.Empty<SceneAsset>();
		#else
		public string[] GetScenes() 
			=> scenes ?? Array.Empty<string>();
		#endif

		public static bool Check(IWorldDescriptor descriptor) {
			var modules = descriptor.GetModules<ScenesWorldModule>();

			var module = modules.Length switch {
				1 => modules.FirstOrDefault(),
				0 => descriptor.Anchor.AddComponent<ScenesWorldModule>(),
				_ => null
			};

			if (!module) {
				Logger.LogError("Verify that the World prefab has a valid FellInVoidWorldModule component.");
				return false;
			}

			return true;
		}

		public UniTask<bool> Setup(IRuntimeWorld runtime) 
			=> UniTask.FromResult(true);
	}
}
