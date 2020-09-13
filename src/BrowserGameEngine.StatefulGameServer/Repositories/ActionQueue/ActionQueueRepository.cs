using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrowserGameEngine.StatefulGameServer.Repositories.Actions {
	public class ActionQueueRepository {
		private readonly WorldState world;
		private readonly PlayerRepository playerRepository;

		public ActionQueueRepository(WorldState world, PlayerRepository playerRepository) {
			this.world = world;
			this.playerRepository = playerRepository;
		}


	}
}
