using System.Collections.Generic;
using System.Linq;
using BrowserGameEngine.GameModel;

namespace BrowserGameEngine.StatefulGameServer {
	public class AssetRepository {
		private readonly WorldState world;
		private IDictionary<PlayerId, List<AssetState>> Assets => world.Assets;

		public AssetRepository(WorldState world) {
			this.world = world;
		}

		public IEnumerable<AssetState> Get(PlayerId playerId) {
			if (Assets.TryGetValue(playerId, out List<AssetState> result)) {
				return result;
			}
			return Enumerable.Empty<AssetState>();
		}
	}
}