using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.Commands;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using System.Collections.Generic;

namespace BrowserGameEngine.StatefulGameServer {
	public class AssetRepositoryWrite {
		private readonly WorldState world;
		private IDictionary<PlayerId, List<AssetState>> Assets => world.Assets;

		public AssetRepositoryWrite(WorldState world) {
			this.world = world;
		}
	}
}