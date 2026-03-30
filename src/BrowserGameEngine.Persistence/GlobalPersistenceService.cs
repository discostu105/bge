using BrowserGameEngine.GameModel;
using System.Threading.Tasks;

namespace BrowserGameEngine.Persistence {
	public class GlobalPersistenceService {
		private const string filename = "global/state.json";
		private readonly IBlobStorage storage;
		private readonly GlobalStateJsonSerializer serializer;

		public GlobalPersistenceService(IBlobStorage storage, GlobalStateJsonSerializer serializer) {
			this.storage = storage;
			this.serializer = serializer;
		}

		public async Task<GlobalStateImmutable> LoadGlobalState() {
			return serializer.Deserialize(await storage.Load(filename));
		}

		public async Task StoreGlobalState(GlobalStateImmutable state) {
			await storage.Store(filename, serializer.Serialize(state));
		}

		public bool GlobalStateExists() => storage.Exists(filename);
	}
}
