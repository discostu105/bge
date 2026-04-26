using System;
using System.Collections.Generic;
using System.Linq;
using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;

namespace BrowserGameEngine.StatefulGameServer {
	public class PlayerRepository {
		private readonly IWorldStateAccessor worldStateAccessor;
		private WorldState world => worldStateAccessor.WorldState;
		private readonly ResourceRepository resourceRepository;
		private readonly AllianceRepository allianceRepository;

		private IDictionary<PlayerId, Player> Players => world.Players;

		public PlayerRepository(IWorldStateAccessor worldStateAccessor
			, ResourceRepository resourceRepository
			, AllianceRepository allianceRepository) {
			this.worldStateAccessor = worldStateAccessor;
			this.resourceRepository = resourceRepository;
			this.allianceRepository = allianceRepository;
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

		private static decimal GetMinLand(decimal attackerLand) {
			return attackerLand * 0.5M; // TODO: selection shall be configurable behavior
		}

		public bool IsPlayerAttackable(PlayerId attacker, PlayerId defender) {
			return GetIneligibilityReason(attacker, defender) == null;
		}

		public AttackIneligibilityReason? GetIneligibilityReason(PlayerId attacker, PlayerId defender) {
			if (attacker == defender) return AttackIneligibilityReason.SelfAttack;
			if (IsProtected(attacker)) return AttackIneligibilityReason.AttackerProtected;
			if (IsProtected(defender)) return AttackIneligibilityReason.DefenderProtected;
			if (AreAllied(attacker, defender)) return AttackIneligibilityReason.SameAlliance;
			var attackerLand = resourceRepository.GetLand(attacker);
			var defenderLand = resourceRepository.GetLand(defender);
			if (defenderLand < GetMinLand(attackerLand)) return AttackIneligibilityReason.LandTooSmall;
			return null;
		}

		private bool IsProtected(PlayerId id) {
			return world.GetPlayer(id).State.ProtectionTicksRemaining > 0;
		}

		private bool AreAllied(PlayerId a, PlayerId b) {
			return allianceRepository.IsSameAlliance(a, b);
		}

		public bool Exists(PlayerId playerId) {
			return world.PlayerExists(playerId);
		}

		public int GetGasPercent(PlayerId playerId) => world.GetPlayer(playerId).State.GasPercent;

		/// <summary>
		/// Splits <paramref name="totalWorkers"/> across minerals and gas using the player's
		/// configured gas percentage. The split is computed on the fly — we no longer track
		/// absolute mineral/gas worker counts.
		/// </summary>
		public (int MineralWorkers, int GasWorkers) GetWorkerAssignment(PlayerId playerId, int totalWorkers) {
			int gasPercent = Math.Clamp(GetGasPercent(playerId), 0, 100);
			int gas = (int)Math.Round(totalWorkers * gasPercent / 100.0, MidpointRounding.AwayFromZero);
			gas = Math.Clamp(gas, 0, totalWorkers);
			int minerals = totalWorkers - gas;
			return (minerals, gas);
		}
	}
}