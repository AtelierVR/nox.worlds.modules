using System;
using Nox.Worlds;
using Nox.CCK.Worlds.FellInVoid;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Events;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Worlds.Scenes;
using Nox.CCK.Worlds.Spawns;

namespace Nox.Worlds.Modules.Runtime {
	public class Main : IMainModInitializer {
		internal static IMainModCoreAPI      CoreAPI;
		private         EventSubscription[] _events;

		public void OnInitializeMain(IMainModCoreAPI api) {
			CoreAPI = api;
			_events = new[] {
				api.EventAPI.Subscribe("world_check_request", OnCheckRequest),
			};
		}

		private static void OnCheckRequest(EventData context) {
			if (!context.TryGet<IWorldDescriptor>(0, out var descriptor))
				return;
			var valid = true;
			valid &= FellInVoidWorldModule.Check(descriptor);
			valid &= SpawnsWorldModule.Check(descriptor);
			valid &= ScenesWorldModule.Check(descriptor);
			context.Callback(valid);
		}

		public void OnDisposeMain() {
			foreach (var e in _events)
				CoreAPI.EventAPI.Unsubscribe(e);
			_events = Array.Empty<EventSubscription>();
			CoreAPI = null;
		}
	}
}
