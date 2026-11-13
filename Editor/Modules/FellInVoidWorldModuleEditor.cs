using Nox.CCK.Editor;
using Nox.CCK.Language;
using Nox.CCK.Worlds.FellInVoid;
using Nox.Search;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nox.Editor.Worlds.FellInVoidWorld {
	public class FellInVoidWorldModuleEditor : LocalizedInspectorEditor<FellInVoidWorldModule> {
		/// <summary>Prefix of the language entries of this inspector (see the mod lang.asset).</summary>
		private const string LangPrefix = "worlds.modules.fell_in_void.inspector";

		/// <summary>Refresh period of the local player position, in milliseconds.</summary>
		private const long PlayerRefreshPeriod = 250;

		/// <summary>Icon of the component: the « south » glyph rendered in the assets of the <c>ui</c> mod.</summary>
		protected override ResourceTextureSource Icon
			=> "ui:icons/fell_in_void.png";

		protected override string TitleKey
			=> LangPrefix + ".title";

		/// <summary>The field of the fall threshold, kept to localize its label on a language change.</summary>
		private PropertyField _threshold;

		/// <summary>The notification showing the position of the local player.</summary>
		private LocalizedNotificationElement _player;

		/// <summary>The timer refreshing that position: it only runs while the inspector is displayed.</summary>
		private IVisualElementScheduledItem _timer;

		public override VisualElement CreateInspectorGUI() {
			var root = base.CreateInspectorGUI();
			if (Content == null)
				return root;

			// The GUI is rebuilt on every hierarchy change while the base keeps its root: never add twice.
			Content.Clear();

			var uxml = Resources.Load<VisualTreeAsset>(nameof(FellInVoidWorldModuleEditor));
			if (uxml == null) {
				Content.Add(new Label($"The {nameof(FellInVoidWorldModuleEditor)} uxml template is not found in Resources."));
				return root;
			}

			var ui = uxml.CloneTree();
			Content.Add(ui);

			// Fields are bound by name: an InspectorElement(serializedObject) would run this very editor again.
			_threshold = BindField(ui, nameof(FellInVoidWorldModule.fallThreshold));

			_player = ui.Q<LocalizedNotificationElement>("player");

			// The position only moves at runtime: a timer refreshes the label. It stays paused until the inspector
			// is displayed — see UpdatePlayerTimer and InspectorEditor<T>.IsVisible.
			_timer = ui.schedule.Execute(RefreshPlayer).Every(PlayerRefreshPeriod);
			_timer.Pause();
			UpdatePlayerTimer();

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

			if (_threshold != null)
				_threshold.label = LanguageManager.Get(LangPrefix + ".threshold");
		}

		/// <summary>
		/// Runs the refresh timer only while the inspector is displayed: a window scrolled away, collapsed or
		/// closed stops the polling instead of reading the player every tick for nothing.
		/// </summary>
		private void UpdatePlayerTimer() {
			if (_timer == null)
				return;

			if (!IsVisible) {
				_timer.Pause();
				return;
			}

			// Refresh before resuming, so the label is up to date as soon as the inspector shows up again.
			RefreshPlayer();
			_timer.Resume();
		}

		/// <summary>The inspector started being displayed: pick the player position up again.</summary>
		protected override void OnVisible()
			=> UpdatePlayerTimer();

		/// <summary>The inspector is hidden, collapsed or scrolled away: stop polling the player.</summary>
		protected override void OnInvisible()
			=> UpdatePlayerTimer();

		/// <summary>
		/// Shows the position of the local player. The module only knows it during a session, so the label says
		/// so between sessions.
		/// </summary>
		private void RefreshPlayer() {
			if (_player == null)
				return;

			var player = Target.LocalPlayer;
			if (player == null) {
				_player.LangKey = LangPrefix + ".no_player";
				return;
			}

			_player.LangKey  = LangPrefix + ".player";
			_player.LangArgs = new object[] { player.Position };
		}
	}
}
