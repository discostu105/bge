using System.Collections.Generic;
using BrowserGameEngine.GameModel;

namespace BrowserGameEngine.StatefulGameServer {
	public class PlayerReadApi {
		private readonly PlayerRepository playerRepository;

		public PlayerReadApi(PlayerRepository playerRepository) {
			this.playerRepository = playerRepository;
		}

		public Player Get(PlayerId playerId) {
			return playerRepository.Players[playerId];
		}

		public IEnumerable<Player> GetAll() {
			return playerRepository.Players.Values;
		}

		public void Add(Player player) {
			playerRepository.Players.Add(player.PlayerId, player);
		}
	}
}