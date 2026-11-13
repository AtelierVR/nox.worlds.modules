using System;
using System.Linq;
using Nox.Assets.Editor;
using Nox.CCK.Worlds;
using Nox.Worlds;
using UnityEngine;

namespace Nox.Editor.Worlds.Modules {
	/// <summary>
	/// Plumbing shared by the module notifications (one file per check, see the others): they all write in
	/// the shared list (<see cref="AssetNotificationHelper"/>) with the same shape — a language key first,
	/// then its arguments — and can all point the author at the module they are about.
	/// <para>
	/// The writing itself is <see cref="AssetModuleNotification"/> (shared with the avatar modules); this
	/// class only says which scope the world uses and what a module of a world is.
	/// </para>
	/// </summary>
	internal static class WorldModuleNotification {
		/// <summary>Panel id of the world publisher/builder, plus the keys of the generic sentences.</summary>
		public static readonly AssetModuleNotificationScope Scope = new("world", "worlds.modules.notification");

		/// <summary>
		/// Modules of the world. <see cref="WorldDescriptor.Modules"/> is only filled by a compile, so the
		/// scene is rescanned here: <see cref="WorldDescriptor.FindModules(IWorldDescriptor)"/> is the one
		/// place that knows what a module of a world is.
		/// </summary>
		public static T[] Modules<T>(WorldDescriptor world) where T : class, IWorldModule
			=> world
				? WorldDescriptor.FindModules(world).OfType<T>().ToArray()
				: Array.Empty<T>();

		/// <summary>Writes (or replaces) the entry of <paramref name="uid"/>.</summary>
		public static void Show(
			string uid,
			AssetNotificationType type,
			string key,
			object[] args = null,
			params AssetNotificationAction[] actions
		)
			=> AssetModuleNotification.Show(Scope, uid, type, key, args, actions);

		/// <summary>Removes the entry of <paramref name="uid"/> (nothing to report any more).</summary>
		public static void Hide(string uid)
			=> AssetModuleNotification.Hide(uid);

		/// <summary>Reports a world with several modules of the same type.</summary>
		public static void ShowDuplicated(string uid, string module, int count, Component first)
			=> AssetModuleNotification.ShowDuplicated(Scope, uid, module, count, first);

		/// <summary>Reports a missing module and offers to add it to the world anchor.</summary>
		public static void ShowMissing<T>(
			string uid,
			WorldDescriptor world,
			string module,
			string messageKey,
			Action refresh,
			params object[] args
		) where T : Component
			=> AssetModuleNotification.ShowMissing<T>(
				Scope,
				uid,
				world ? world.Anchor : null,
				module,
				messageKey,
				refresh,
				args
			);

		/// <summary>Action that selects <paramref name="module"/>, so the author finds what is reported.</summary>
		public static AssetNotificationAction Select(Component module)
			=> AssetModuleNotification.Select(Scope, module);
	}
}
