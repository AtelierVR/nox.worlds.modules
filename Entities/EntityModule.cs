using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using Nox.Entities;
using Nox.Players;
using Nox.Sessions;
using Nox.Worlds;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.CCK.Worlds.Entities {
	/// <summary>
	/// Publishes the entities of the world to the network entity channel and resolves the entity spawns
	/// announced by the server against them.
	/// <para>
	/// Every <see cref="IEntity"/> of the world is discovered: a world object carrying a networked behaviour
	/// (<see cref="ISessionEntity"/>: a video player, a grabbable, ...) is set up with the session when it is
	/// found. A pre-placed entity has a stable type (the CRC32 of its path in the world, identical on every
	/// client): the session master announces it with that type and the server assigns the id, so each
	/// client maps the spawn back onto its own copy. The entity then lives in the session entity list like
	/// any other entity (transform channel, properties channel, ownership).
	/// </para>
	/// </summary>
	[DisallowMultipleComponent]
	public sealed class EntityModule : MonoBehaviour, ISessionModule {
		/// <summary>
		/// Whether the world of the descriptor already has its module, adding it when it has none.
		/// </summary>
		public static bool Check(IWorldDescriptor descriptor) {
			var modules = descriptor.GetModules<EntityModule>();

			switch (modules.Length) {
				case 1:
					return true;
				case 0:
					descriptor.Anchor.AddComponent<EntityModule>();
					return true;
				default:
					Logger.LogError("A world can only have one EntityModule.", tag: nameof(EntityModule));
					return false;
			}
		}

		private readonly Dictionary<uint, IEntity> _world = new();
		private readonly HashSet<IEntity> _entities = new();

		private static readonly List<EntityModule> Actives = new();

		private IRuntimeWorld _runtime;
		private ISession _session;
		private ISessionSpawner _spawner;

		/// <summary>
		/// Answer an entity spawn with the world object of the matching type (first answer wins). Called by
		/// the mod answering <see cref="IEntitySpawn.EventName"/>, which owns the event bus.
		/// </summary>
		public static bool Resolve(IEntitySpawn spawn) {
			if (spawn == null || spawn.Answered)
				return false;

			for (var i = 0; i < Actives.Count; i++)
				if (Actives[i].TryResolve(spawn))
					return true;

			return false;
		}

		private bool TryResolve(IEntitySpawn spawn) {
			if (_session == null || !ReferenceEquals(spawn.Session, _session))
				return false;

			if (!_world.TryGetValue(spawn.Type, out var entity))
				return false;

			entity.Id = spawn.Id;
			spawn.Callback(entity);
			return true;
		}

		private static bool IsMaster(ISession session)
			=> session?.MasterPlayer is { IsLocal: true };

		public UniTask<bool> Setup(IRuntimeWorld runtime) {
			_runtime = runtime;

			if (!Actives.Contains(this))
				Actives.Add(this);

			return UniTask.FromResult(true);
		}

		public void OnLoaded(ISession session) {
			_session?.OnStateChanged.RemoveListener(OnSessionStateChanged);

			_session = session;
			_spawner = session as ISessionSpawner;
			session.OnStateChanged.AddListener(OnSessionStateChanged);

			SetupEntities();
			Discover();
		}

		/// <summary>
		/// Publishes once the session is ready. <see cref="OnConnected"/> arrives when the socket is up, which
		/// can be before the world is loaded and before the master is known: this callback is the only one
		/// that is always fired after the enter, so it is the one that never misses the world entities.
		/// </summary>
		private void OnSessionStateChanged(IState state) {
			if (state is not { Status: Status.Ready })
				return;

			PublishAll();
			Sync();
		}

		/// <summary>
		/// Ask the server for the entities created before this client joined: the master announces what it
		/// creates and the relay broadcasts a create to the clients of the room, so a client joining later
		/// only knows them by asking. Nothing to ask when this client is the master: it publishes its own.
		/// </summary>
		private void Sync() {
			if (_session?.State?.Status != Status.Ready || IsMaster(_session))
				return;

			_spawner?.SyncEntities().Forget();
		}

		public void OnConnected()
			=> PublishAll();

		public void OnAuthorityTransferred(IPlayer current, IPlayer previous) {
			PublishAll();
			NotifyOwnershipChanged(current, previous);
		}

		/// <summary>
		/// An entity that follows the authority belongs to the new master now: its listeners are told. Nothing
		/// is rewritten, the owner is resolved live from the session.
		/// </summary>
		private void NotifyOwnershipChanged(IPlayer current, IPlayer previous) {
			foreach (var entity in _entities) {
				if (!IsAlive(entity) || entity is not IOwnedEntity owned)
					continue;

				if (!ReferenceEquals(owned.Owner, current))
					continue;

				owned.OnOwnershipChanged.Invoke(current, previous);
			}
		}

		public void OnSessionDeselected() {
			_session?.OnStateChanged.RemoveListener(OnSessionStateChanged);
			_session = null;
			_spawner = null;
		}

		public void OnSceneLoaded(IWorldDescriptor descriptor, int index, GameObject anchor) {
			if (anchor)
				Track(anchor);

			Sync();
		}

		public void OnSceneUnloaded(int index) { }

		private void OnDestroy() {
			_session?.OnStateChanged.RemoveListener(OnSessionStateChanged);
			Actives.Remove(this);
			_entities.Clear();
			_world.Clear();
		}

		private void OnEnable() {
			if (!Actives.Contains(this))
				Actives.Add(this);

			InstantiateHelper.OnInstantiate.AddListener(OnInstantiated);
		}

		private void OnDisable() {
			InstantiateHelper.OnInstantiate.RemoveListener(OnInstantiated);
			Actives.Remove(this);
		}

		/// <summary>
		/// An object was instantiated at runtime (a prefab spawned by the world, a prop, a video player):
		/// its entities are discovered like the ones of the scene, otherwise a local entity created after
		/// the world was loaded would never be published.
		/// </summary>
		private void OnInstantiated(GameObject instance) {
			if (instance)
				Track(instance);
		}

		private void Discover() {
			if (_runtime == null)
				return;

			foreach (var dimension in _runtime.Dimensions) {
				var ids = dimension.GetInstanceIds();
				for (var i = 0; i < ids.Length; i++) {
					var anchor = dimension.GetAnchor(ids[i]);
					if (anchor)
						Track(anchor);
				}
			}

			Sync();
		}

		/// <summary>Register every <see cref="IEntity"/> under the given root (players, world objects, ...).</summary>
		private void Track(GameObject root) {
			var found = root.GetComponentsInChildren<IEntity>(true);
			for (var i = 0; i < found.Length; i++)
				Register(found[i]);
		}

		private void Register(IEntity entity) {
			if (entity == null || !_entities.Add(entity))
				return;

			if (_session != null && entity is ISessionEntity sessionEntity)
				sessionEntity.Setup(_session);

			var type = TypeOf(entity);
			if (type == 0 || !_world.TryAdd(type, entity))
				return;

			Logger.LogDebug($"Registered entity {entity.GetType().Name} as type {type}", tag: nameof(EntityModule));

			Publish(type, entity);

			if (entity is not Component component)
				return;

			var destroy = component.gameObject.GetOrAddComponent<DestroyComponent>();
			destroy.Destroyed.AddListener(() => OnEntityDestroyed(entity));
		}

		/// <summary>
		/// Stable type of an entity: the one it declares, or the CRC32 of its hierarchy path. It has to be the
		/// same on every client, so it never derives from anything local (a scene path, an instance id).
		/// </summary>
		private static uint TypeOf(IEntity entity)
			=> entity is IEntityType typed
				? typed.Type
				: entity is Component component
					? WorldPath.DeriveType(component.transform)
					: 0;

		/// <summary>Set up the entities found before the session was known.</summary>
		private void SetupEntities() {
			if (_session == null)
				return;

			foreach (var entity in _entities)
				if (entity is ISessionEntity sessionEntity)
					sessionEntity.Setup(_session);
		}

		/// <summary>Announce to the server every entity it does not know yet (the master owns the create).</summary>
		private void PublishAll() {
			if (_session == null || !IsMaster(_session))
				return;

			foreach (var (type, entity) in _world)
				Publish(type, entity);
		}

		/// <summary>
		/// Announce an entity as soon as the session can create it. A world registered after the session became
		/// ready never saw <see cref="PublishAll"/> (the enter comes before the world is loaded), so the
		/// registration is the second trigger: without it a master loading its world late creates nothing.
		/// </summary>
		private void Publish(uint type, IEntity entity) {
			if (_session?.State?.Status != Status.Ready || !IsMaster(_session))
				return;

			if (!IsAlive(entity) || entity.Id != 0)
				return;

			Create(type).Forget();
		}

		private async UniTaskVoid Create(uint type) {
			var spawner = _spawner;
			if (spawner == null) {
				Logger.LogWarning($"Cannot publish entity type {type}: the session has no spawner.", tag: nameof(EntityModule));
				return;
			}

			Logger.LogDebug($"Publishing entity type {type}", tag: nameof(EntityModule));
			await spawner.CreateEntity(type);
		}

		private static bool IsAlive(IEntity entity)
			=> entity != null && (entity is not Component component || component);

		private void OnEntityDestroyed(IEntity entity) {
			_entities.Remove(entity);

			if (entity == null || !_world.Remove(TypeOf(entity), out var known) || !ReferenceEquals(known, entity))
				return;

			var created = Resolve(entity);
			if (created == null || !IsMaster(_session) || _spawner == null)
				return;

			_spawner.RemoveEntity(created).Forget();
		}

		private IEntity Resolve(IEntity entity)
			=> entity.Id == 0 || _session == null
				? null
				: _session.Entities.GetEntity(entity.Id);
	}
}
