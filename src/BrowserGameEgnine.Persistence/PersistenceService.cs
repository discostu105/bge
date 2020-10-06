using BrowserGameEngine.GameModel;
using System.Threading.Tasks;

namespace BrowserGameEgnine.Persistence {
	public class PersistenceService {
		private readonly IBlobStorage storage;
		private readonly GameStateJsonSerializer serializer;

		public PersistenceService(IBlobStorage storage, GameStateJsonSerializer serializer) {
			this.storage = storage;
			this.serializer = serializer;
		}

		public async Task<WorldStateImmutable> LoadWorldState() {
			return serializer.Deserialize(await storage.Load("latest"));
		}

		public async Task StoreWorldSate(WorldStateImmutable worldStateImmutable) {
			await storage.Store("latest", serializer.Serialize(worldStateImmutable));
		}

		public bool WorldStateExists() {
			return storage.Exists("latest");
		}
	}
}
