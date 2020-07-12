using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.Commands;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using System.Collections.Generic;

namespace BrowserGameEngine.StatefulGameServer {
	public class UnitRepositoryWrite {
		private readonly WorldState world;
		private IDictionary<PlayerId, List<UnitState>> Units => world.Units;

		public UnitRepositoryWrite(WorldState world) {
			this.world = world;
		}
	}
}