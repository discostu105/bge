using System.Collections.Generic;
using BrowserGameEngine.GameModel;

namespace BrowserGameEngine.StatefulGameServer {
	public class PlayerReadApi {
		private readonly PlayerRepository playerRepository;

		public PlayerReadApi(PlayerRepository playerRepository) {
			this.playerRepository = playerRepository;
		}

		public Player Get(PlayerId playerId) {
			return null;
		}

		public IEnumerable<Player> GetAll() {
			return null;
		}

		public void Add(Player player) {

		}
	}
}