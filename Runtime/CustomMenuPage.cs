using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using Nox.CCK.Worlds.CustomMenu;
using Nox.UI;
using UnityEngine;

namespace Nox.Worlds.Modules.Runtime {
	/// <summary>
	/// Menu page hosting the prefab provided by the world (see <see cref="CustomMenuModule"/>).
	/// <para>
	/// Visibility is tracked from both sides because the menu can hide the page either by navigating
	/// elsewhere (<see cref="OnDisplay"/>/<see cref="OnHide"/>) or by closing while the page stays
	/// current. nox.ui exposes no visibility event, so the page watches the menu state while it is
	/// displayed. The module and the <c>world_custom_shown</c>/<c>world_custom_hidden</c> events are
	/// notified only on change.
	/// </para>
	/// </summary>
	public class CustomMenuPage : IPage {
		/// <summary>How often the menu visibility is checked while the page is displayed.</summary>
		private const int VisibilityPollMs = 100;

		private readonly int _menuId;
		private readonly object[] _context;
		private GameObject _content;
		private bool _shown;
		private CancellationTokenSource _watch;

		internal CustomMenuModule Module { get; private set; }

		internal CustomMenuPage(int menuId, CustomMenuModule module, object[] context) {
			_menuId  = menuId;
			Module   = module;
			_context = context ?? Array.Empty<object>();
		}

		public string GetKey()
			=> Client.PageKey;

		public object[] GetContext()
			=> _context;

		public IMenu GetMenu()
			=> Client.UiAPI?.Get<IMenu>(_menuId);

		public GameObject GetContent(RectTransform parent) {
			if (_content)
				return _content;
			if (!Module || !Module.prefab)
				return null;

			_content      = Module.prefab.Instantiate(parent);
			_content.name = $"[{GetKey()}_{_content.GetId()}]";

			// Instantiate keeps the anchors of the prefab; the page fills the menu content area.
			if (_content.transform is RectTransform rect) {
				rect.anchorMin = Vector2.zero;
				rect.anchorMax = Vector2.one;
				rect.offsetMin = Vector2.zero;
				rect.offsetMax = Vector2.zero;
				rect.pivot     = new Vector2(0.5f, 0.5f);
				rect.localScale = Vector3.one;
			}

			return _content;
		}

		public void OnDisplay(IPage lastPage) {
			SetShown(GetMenu()?.Active ?? true);
			WatchVisibility();
		}

		public void OnHide(IPage nextPage) {
			StopWatch();
			SetShown(false);
		}

		public void OnRemove() {
			StopWatch();
			SetShown(false);
			if (_content)
				_content.Destroy();
			_content = null;
		}

		private void SetShown(bool shown) {
			if (_shown == shown)
				return;
			_shown = shown;

			if (Module) {
				if (shown)
					Module.onMenuShown?.Invoke();
				else
					Module.onMenuHidden?.Invoke();
			}

			Client.Emit(
				shown ? Client.ShownEvent : Client.HiddenEvent,
				Module ? Module.gameObject : null,
				_content,
				_menuId
			);
		}

		#region Visibility watch

		/// <summary>
		/// Polls the menu visibility while the page is displayed: only the open custom menu page runs a
		/// watch, and toggling the menu is user driven, so the cost is irrelevant.
		/// </summary>
		private void WatchVisibility() {
			StopWatch();
			_watch = new CancellationTokenSource();
			WatchVisibilityAsync(_watch.Token).Forget();
		}

		private async UniTaskVoid WatchVisibilityAsync(CancellationToken token) {
			try {
				while (true) {
					await UniTask.Delay(VisibilityPollMs, cancellationToken: token);

					var menu = GetMenu();
					if (menu == null) {
						// Menu disposed: nothing left to observe.
						SetShown(false);
						return;
					}

					if (!Module) {
						// The world providing the page is gone: leave the page before it becomes inert.
						SetShown(false);
						menu.GoBack();
						if (menu.GetCurrent() == this)
							Client.UiAPI?.SendGoto(menu.Id, "home");
						return;
					}

					SetShown(menu.Active);
				}
			} catch (OperationCanceledException) {
				// Page hidden or removed.
			}
		}

		private void StopWatch() {
			if (_watch == null)
				return;
			_watch.Cancel();
			_watch.Dispose();
			_watch = null;
		}

		#endregion
	}
}
