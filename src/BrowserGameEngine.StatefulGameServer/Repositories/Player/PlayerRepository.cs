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
			return Players
				.Where(x => GetIneligibilityReason(playerId, x.Key) == null)
				.Select(x => x.Value.ToImmutable());
		}

		private static decimal GetMinScore(decimal playerScore) {
			return playerScore * 0.5M; // TODO: selection shall be configurable behavior
		}

		public bool IsPlayerAttackable(PlayerId attacker, PlayerId defender) {
			return GetIneligibilityReason(attacker, defender) == null;
		}

		public AttackIneligibilityReason? GetIneligibilityReason(PlayerId attacker, PlayerId defender) {
			if (attacker == defender) return AttackIneligibilityReason.SelfAttack;
			if (IsProtected(attacker)) return AttackIneligibilityReason.AttackerProtected;
			if (IsProtected(defender)) return AttackIneligibilityReason.DefenderProtected;
			if (AreAllied(attacker, defender)) return AttackIneligibilityReason.SameAlliance;
			var attackerScore = scoreRepository.GetScore(attacker);
			var defenderScore = scoreRepository.GetScore(defender);
			if (defenderScore < GetMinScore(attackerScore)) return AttackIneligibilityReason.LandTooSmall;
			return null;
		}

		private bool IsProtected(PlayerId id) {
			return world.GetPlayer(id).State.ProtectionTicksRemaining > 0;
		}

		private bool AreAllied(PlayerId a, PlayerId b) {
			// BGE-33: check alliance membership when alliances are implemented
			return false;
		}

		public bool Exists(PlayerId playerId) {
			return world.PlayerExists(playerId);
		}

		public int GetMineralWorkers(PlayerId playerId) => world.GetPlayer(playerId).State.MineralWorkers;
		public int GetGasWorkers(PlayerId playerId) => world.GetPlayer(playerId).State.GasWorkers;
	}
}