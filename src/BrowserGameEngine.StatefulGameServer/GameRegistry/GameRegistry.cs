using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace BrowserGameEngine.StatefulGameServer.GameRegistry {
	public class GameRegistry {
		public GlobalState GlobalState { get; }
		private readonly ConcurrentDictionary<string, GameInstance> _instances = new();

		public GameRegistry(GlobalState globalState) { GlobalState = globalState; }

		public void Register(GameInstance instance) => _instances[instance.Record.GameId.Id] = instance;

		public GameInstance? TryGetInstance(GameId gameId) =>
			_instances.TryGetValue(gameId.Id, out var i) ? i : null;

		public IReadOnlyCollection<GameInstance> GetAllInstances() => (IReadOnlyCollection<GameInstance>)_instances.Values;

		public GameInstance GetDefaultInstance() => _instances.Values.First();
	}
}
