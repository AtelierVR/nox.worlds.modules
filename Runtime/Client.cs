using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Events;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Utils;
using Nox.CCK.Worlds.CustomMenu;
using Nox.UI;
using Nox.Worlds;
using UnityEngine;

namespace Nox.Worlds.Modules.Runtime {
	/// <summary>
	/// Client side of the custom menu page: adds the button of the current world in every menu and
	/// creates the <see cref="CustomMenuPage"/> when the menu navigates to it.
	/// </summary>
	public class Client : IClientModInitializer {
		/// <summary>Key of the page, navigated with the <c>menu_goto</c> event.</summary>
		internal const string PageKey = "world.custom";

		/// <summary>Emitted when a menu shows the custom page (module <c>onMenuShown</c>).</summary>
		internal const string ShownEvent = "world_custom_shown";

		/// <summary>Emitted when a menu hides the custom page (module <c>onMenuHidden</c>).</summary>
		internal const string HiddenEvent = "world_custom_hidden";

		private const string MenuAddedEvent    = "menu_added";
		private const string MenuRemovedEvent  = "menu_removed";
		private const string MenuGotoEvent     = "menu_goto";
		private const string SceneAddedEvent   = "session_scene_added";
		private const string SceneRemovedEvent = "session_scene_removed";

		/// <summary>Part of the menu that receives the custom page button.</summary>
		private const string NavigationPartKey = "applications";

		/// <summary>Icon used when the module does not set one.</summary>
		private const string DefaultIcon = "ui:icons/apps.png";

		internal static IClientModCoreAPI CoreAPI;

		internal static IUiAPI UiAPI
			=> CoreAPI?.ModAPI.GetMod("ui")?.GetInstance<IUiAPI>();

		internal static void Emit(string eventName, params object[] data)
			=> CoreAPI?.EventAPI.Emit(eventName, data);

		private EventSubscription[] _events;

		/// <summary>Menus currently alive, in creation order.</summary>
		private readonly List<IMenu> _menus = new();

		/// <summary>
		/// Ids of the menus whose parts are initialized: <c>Menu.Start</c> emits its first
		/// <c>menu_goto</c> only once every part has its <c>menu</c> reference set. Before that,
		/// adding an element yields one without icon/click.
		/// </summary>
		private readonly HashSet<int> _ready = new();

		/// <summary>Page key of the button added to each menu, so a world change can replace it.</summary>
		private readonly Dictionary<int, string> _keys = new();

		/// <summary>Custom menu page created for each menu (several menus may show the page at once).</summary>
		private readonly Dictionary<int, CustomMenuPage> _pages = new();

		/// <summary>
		/// Module of the loaded world, resolved from the world scene descriptor when the scene is
		/// loaded, keyed by scene index (a portal may load another scene).
		/// </summary>
		private readonly Dictionary<int, CustomMenuModule> _modules = new();

		/// <summary>Module of the main loaded scene, or <c>null</c> when the world has no custom menu.</summary>
		private CustomMenuModule CurrentModule {
			get {
				var bestIndex = int.MaxValue;
				CustomMenuModule module = null;

				foreach (var pair in _modules) {
					if (!pair.Value || pair.Key >= bestIndex)
						continue;
					bestIndex = pair.Key;
					module    = pair.Value;
				}

				return module;
			}
		}

		public void OnInitializeClient(IClientModCoreAPI api) {
			CoreAPI = api;
			_events = new[] {
				api.EventAPI.Subscribe(MenuAddedEvent, OnMenuAdded),
				api.EventAPI.Subscribe(MenuRemovedEvent, OnMenuRemoved),
				api.EventAPI.Subscribe(MenuGotoEvent, OnMenuGoto),
				api.EventAPI.Subscribe(SceneAddedEvent, OnSceneAdded),
				api.EventAPI.Subscribe(SceneRemovedEvent, OnSceneRemoved),
			};
		}

		public void OnDisposeClient() {
			if (CoreAPI != null)
				foreach (var subscription in _events)
					CoreAPI.EventAPI.Unsubscribe(subscription);

			_events = Array.Empty<EventSubscription>();
			_menus.Clear();
			_ready.Clear();
			_keys.Clear();
			_pages.Clear();
			_modules.Clear();
			CoreAPI = null;
		}

		#region Module

		/// <summary>
		/// A world scene was loaded: the module is resolved from the scene descriptor (its
		/// <see cref="IWorldDescriptor.Anchor"/> subtree), not by scanning every object of the process.
		/// The anchor is used as a fallback when the descriptor is not in the event payload.
		/// </summary>
		private void OnSceneAdded(EventData context) {
			if (!context.TryGet(1, out int index))
				return;

			_modules[index] = ResolveModule(context);
			RefreshButtons();
		}

		private void OnSceneRemoved(EventData context) {
			if (!context.TryGet(1, out int index))
				return;

			_modules.Remove(index);
			RefreshButtons();
		}

		/// <summary>Module of the loaded scene: <c>session_scene_added</c> carries (session, index, descriptor, anchor).</summary>
		private static CustomMenuModule ResolveModule(EventData context) {
			if (context.TryGet(2, out IWorldDescriptor descriptor) && descriptor.Anchor &&
			    descriptor.Anchor.TryGetComponentInChildren(out CustomMenuModule module))
				return module;

			if (context.TryGet(3, out GameObject anchor) && anchor &&
			    anchor.TryGetComponentInChildren(out module))
				return module;

			return null;
		}

		private void RefreshButtons() {
			foreach (var menu in _menus)
				if (_ready.Contains(menu.Id))
					EnsureButton(menu);
		}

		#endregion

		#region Menu button

		private void OnMenuAdded(EventData context) {
			if (!context.TryGet(0, out IMenu menu) || menu == null)
				return;
			if (!_menus.Contains(menu))
				_menus.Add(menu);
		}

		private void OnMenuRemoved(EventData context) {
			if (!context.TryGet(0, out IMenu menu) || menu == null)
				return;
			_menus.Remove(menu);
			_ready.Remove(menu.Id);
			_keys.Remove(menu.Id);
			_pages.Remove(menu.Id);
		}

		/// <summary>
		/// Adds (or removes) the custom menu page button in <paramref name="menu"/> depending on the
		/// module of the loaded world. Does nothing when the button is already in the expected state.
		/// </summary>
		private void EnsureButton(IMenu menu) {
			var module = CurrentModule;

			if (module == null || !module.prefab) {
				RemoveButton(menu);
				return;
			}

			var part = menu.GetPart(NavigationPartKey);
			if (part == null)
				return;

			if (_keys.TryGetValue(menu.Id, out var previous)) {
				if (previous == PageKey)
					return;
				// The module was replaced: drop the button of the previous world first.
				part.RemoveElement(previous);
			}

			_keys[menu.Id] = PageKey;

			part.AddElement(
				new NavigationData {
					Key                = PageKey,
					Action             = PageKey,
					Icon               = DefaultIcon,
					icon               = module.icon,
					text               = module.label,
					Flags              = NavigationFlags.Button,
					ExecutionArguments = Array.Empty<object>(),
					executionType      = NavigationExecution.Goto,
				}
			).Forget();
		}

		private void RemoveButton(IMenu menu) {
			if (!_keys.TryGetValue(menu.Id, out var key))
				return;
			_keys.Remove(menu.Id);
			menu.GetPart(NavigationPartKey)?.RemoveElement(key);
		}

		#endregion

		#region Page

		private void OnMenuGoto(EventData context) {
			if (!context.TryGet(0, out int menuId) || !context.TryGet(1, out string key))
				return;

			var menu = UiAPI?.Get<IMenu>(menuId);
			if (menu == null)
				return;

			// The first goto is emitted by Menu.Start once the parts are initialized.
			if (_ready.Add(menuId) && !_menus.Contains(menu))
				_menus.Add(menu);
			EnsureButton(menu);

			if (!string.Equals(key, PageKey, StringComparison.Ordinal))
				return;

			var module = CurrentModule;
			if (module == null)
				return;

			var arguments = context.Data != null && context.Data.Length > 2
				? context.Data[2..]
				: Array.Empty<object>();

			var page = new CustomMenuPage(menuId, module, arguments);
			_pages[menuId] = page;
			UiAPI?.SendDisplay(menuId, page);
		}

		#endregion
	}
}
