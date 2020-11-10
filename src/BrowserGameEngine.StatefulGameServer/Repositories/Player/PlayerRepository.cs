using System;
using System.Collections.Generic;
using System.Linq;
using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;

namespace BrowserGameEngine.StatefulGameServer {
	public class PlayerRepository {
		private readonly WorldState world;
		private readonly ScoreRepository scoreRepository;

		private IDictionary<PlayerId, Player> Players => world.Players;

		public PlayerRepository(WorldState world
			, ScoreRepository scoreRepository) {
			this.world = world;
			this.scoreRepository = scoreRepository;
		}

		public PlayerImmutable Get(PlayerId playerId) {
			return world.GetPlayer(playerId).ToImmutable();
		}

		public IEnumerable<PlayerImmutable> GetAll() {
			return Players.Values.Select(x => x.ToImmutable());
		}

		public PlayerTypeDefId GetPlayerType(PlayerId playerId) {
			return Get(playerId).PlayerType;
		}

		public IEnumerable<PlayerImmutable> GetAttackablePlayers(PlayerId playerId) {
			var playerScore = scoreRepository.GetScore(playerId);
			var minScore = GetMinScore(playerScore);
			return Players
				.Where(x => scoreRepository.GetScore(x.Key) >= minScore)
				.Where(x => x.Key != playerId)
				// TODO: consider alliance members here
				.Select(x => x.Value.ToImmutable());
		}

		private static decimal GetMinScore(decimal playerScore) {
			return playerScore * 0.5M; // TODO: selection shall be configurable behavior
		}

		public bool IsPlayerAttackable(PlayerId attacker, PlayerId defender) {
			var attackerScore = scoreRepository.GetScore(attacker);
			var defenderScore = scoreRepository.GetScore(attacker);
			var minScore = GetMinScore(attackerScore);
			return attacker != defender
				&& defenderScore >= attackerScore
				// TODO: consider alliance members
				;
		}

		public bool Exists(PlayerId playerId) {
			return world.PlayerExists(playerId);
		}
	}
}