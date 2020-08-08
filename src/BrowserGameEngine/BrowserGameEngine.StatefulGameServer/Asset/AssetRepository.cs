using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;

namespace BrowserGameEngine.StatefulGameServer {
	public class AssetRepository {
		private readonly WorldState world;
		private IDictionary<PlayerId, List<AssetState>> Assets => world.Assets;

		public AssetRepository(WorldState world) {
			this.world = world;
		}

		// returns all assets from one player
		public IEnumerable<AssetStateImmutable> Get(PlayerId playerId) {
			if (Assets.TryGetValue(playerId, out List<AssetState> result)) {
				return result.Select(x => x.ToImmutable());
			}
			return Enumerable.Empty<AssetStateImmutable>();
		}

		public bool HasAsset(PlayerId playerId, AssetDefId assetDefId) {
			if (Assets.TryGetValue(playerId, out List<AssetState> result)) {
				return result.Any(x => x.AssetDefId == assetDefId);
			}
			return false;
		}
	}
}