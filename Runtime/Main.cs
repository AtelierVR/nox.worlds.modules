using System;
using Nox.CCK.Language;
using Nox.CCK.Worlds.Entities;
using Nox.CCK.Worlds.FellInVoid;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Events;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Worlds.Scenes;
using Nox.CCK.Worlds.ShaderVariants;
using Nox.CCK.Worlds.Spawns;
using Nox.Sessions;

namespace Nox.Worlds.Modules.Runtime {
	public class Main : IMainModInitializer {
		internal static IMainModCoreAPI      CoreAPI;
		private         EventSubscription[] _events;
		private         LanguagePack        _lang;

		public void OnInitializeMain(IMainModCoreAPI api) {
			CoreAPI = api;
			_events = new[] {
				api.EventAPI.Subscribe("world_check_request", OnCheckRequest),
				api.EventAPI.Subscribe(IEntitySpawn.EventName, OnSpawnRequest),
			};

			// Texts of the mod, from its Assets folder
			// (see Packages/nox.worlds.modules/Assets/nox.worlds.modules/lang.asset).
			_lang = api.AssetAPI.GetAsset<LanguagePack>("lang.asset");
			LanguageManager.AddPack(_lang);
		}

		private static void OnCheckRequest(EventData context) {
			if (!context.TryGet<IWorldDescriptor>(0, out var descriptor))
				return;
			var valid = true;
			valid &= FellInVoidWorldModule.Check(descriptor);
			valid &= SpawnsWorldModule.Check(descriptor);
			valid &= ScenesWorldModule.Check(descriptor);
			valid &= ShaderVariantsWorldModule.Check(descriptor);
			valid &= EntityModule.Check(descriptor);
			context.Callback(valid);
		}

		private static void OnSpawnRequest(EventData context) {
			if (!context.TryGet<IEntitySpawn>(1, out var spawn))
				return;

			EntityModule.Resolve(spawn);
		}
		public void OnDisposeMain() {
			LanguageManager.RemovePack(_lang);
			_lang = null;
			foreach (var e in _events)
				CoreAPI.EventAPI.Unsubscribe(e);
			_events = Array.Empty<EventSubscription>();
			CoreAPI = null;
		}
	}
}
