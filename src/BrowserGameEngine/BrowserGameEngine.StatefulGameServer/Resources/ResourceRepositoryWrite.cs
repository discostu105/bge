using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.Commands;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using System.Collections.Generic;

namespace BrowserGameEngine.StatefulGameServer {
	public class ResourceRepositoryWrite {
		private readonly WorldState world;

		public ResourceRepositoryWrite(WorldState world) {
			this.world = world;
		}
	}
}