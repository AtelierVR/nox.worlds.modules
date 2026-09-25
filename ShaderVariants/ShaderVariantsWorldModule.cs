using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.Worlds;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.CCK.Worlds.ShaderVariants {
	/// <summary>
	/// Warms up the world's shader variant collections while the world is being prepared, so the
	/// materials do not hitch when they first become visible.
	/// <para>
	/// They are referenced here because they cannot be discovered from the AssetBundle: a world
	/// bundle is a streamed scene AssetBundle, on which <c>LoadAllAssets</c> and <c>LoadAsset</c>
	/// throw <see cref="InvalidOperationException"/>. Referenced from the scene, they are loaded
	/// with it and stay reachable through <see cref="variants"/>.
	/// </para>
	/// </summary>
	public class ShaderVariantsWorldModule : MonoBehaviour, IWorldModule {
		#region Internal

		public static bool Check(IWorldDescriptor descriptor) {
			var modules = descriptor.GetModules<ShaderVariantsWorldModule>();

			var module = modules.Length switch {
				1 => modules.FirstOrDefault(),
				0 => descriptor.Anchor.AddComponent<ShaderVariantsWorldModule>(),
				_ => null
			};

			if (!module) {
				Logger.LogError("Verify that the World prefab has a valid ShaderVariantsWorldModule component.");
				return false;
			}

			return true;
		}

		public async UniTask<bool> Setup(IRuntimeWorld runtime) {
			try {
				WarmUp();
			} catch (Exception e) {
				Logger.LogWarning(
					new Exception("Failed to warmup world shader variant collections", e),
					tag: nameof(ShaderVariantsWorldModule));
			}

			// Give the loading screen a frame to render after the warmup.
			await UniTask.Yield();

			return true;
		}

		#endregion

		/// <summary>Collections to warm up when the world loads, in order.</summary>
		public ShaderVariantCollection[] variants = Array.Empty<ShaderVariantCollection>();

		public void WarmUp() {
			if (variants == null)
				return;

			foreach (var collection in variants)
				if (collection && !collection.isWarmedUp)
					collection.WarmUp();
		}
	}
}
