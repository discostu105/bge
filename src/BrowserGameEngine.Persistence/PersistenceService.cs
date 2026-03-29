using BrowserGameEngine.GameModel;
using System.Threading.Tasks;

namespace BrowserGameEngine.Persistence {
	public class PersistenceService {
		private const string filename = "latest.json";
		private readonly IBlobStorage storage;
		private readonly GameStateJsonSerializer serializer;

		public PersistenceService(IBlobStorage storage, GameStateJsonSerializer serializer) {
			this.storage = storage;
			this.serializer = serializer;
		}

		public async Task<WorldStateImmutable> LoadWorldState() {
			return serializer.Deserialize(await storage.Load(filename));
		}

		public async Task StoreWorldSate(WorldStateImmutable worldStateImmutable) {
			await storage.Store(filename, serializer.Serialize(worldStateImmutable));
		}

		public bool WorldStateExists() {
			return storage.Exists(filename);
		}

		public async Task<WorldStateImmutable> LoadGameState(GameId gameId) {
			return serializer.Deserialize(await storage.Load($"games/{gameId.Id}/state.json"));
		}

		public async Task StoreGameState(GameId gameId, WorldStateImmutable state) {
			await storage.Store($"games/{gameId.Id}/state.json", serializer.Serialize(state));
		}

		public bool GameStateExists(GameId gameId) {
			return storage.Exists($"games/{gameId.Id}/state.json");
		}

		public WorldStateImmutable DeserializeLegacy(byte[] blob) => serializer.Deserialize(blob);
	}
}
