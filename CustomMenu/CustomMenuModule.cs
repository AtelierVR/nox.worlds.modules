using UnityEngine;
using UnityEngine.Events;

namespace Nox.CCK.Worlds.CustomMenu {
	/// <summary>
	/// Custom page of the player menu: while this module exists in the loaded world, the client adds a
	/// button for the page in every menu and instantiates <see cref="prefab"/> inside the page content.
	/// Several menus can open the page at the same time.
	/// <para>
	/// Pure data holder: the world module only declares the page content and the button appearance.
	/// <see cref="onMenuShown"/> / <see cref="onMenuHidden"/> are invoked per page, when the menu
	/// showing it is opened/closed or navigates away.
	/// </para>
	/// </summary>
	public class CustomMenuModule : MonoBehaviour {
		[Tooltip("UI prefab displayed inside the menu page (a RectTransform is expected).")]
		public GameObject prefab;

		[Tooltip("Localization key (or raw text) of the menu button.")]
		public string label;

		[Tooltip("Texture of the menu button icon. Empty uses the default ui icon.")]
		public Texture2D icon;

		[Tooltip("Invoked when the menu shows the page.")]
		public UnityEvent onMenuShown = new();

		[Tooltip("Invoked when the menu hides the page.")]
		public UnityEvent onMenuHidden = new();
	}
}
