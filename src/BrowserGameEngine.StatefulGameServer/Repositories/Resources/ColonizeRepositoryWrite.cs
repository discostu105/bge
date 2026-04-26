using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.Commands;
using System;

namespace BrowserGameEngine.StatefulGameServer {
	public class ColonizeRepositoryWrite {
		private readonly ResourceRepository resourceRepository;
		private readonly ResourceRepositoryWrite resourceRepositoryWrite;

		public ColonizeRepositoryWrite(
			ResourceRepository resourceRepository,
			ResourceRepositoryWrite resourceRepositoryWrite
		) {
			this.resourceRepository = resourceRepository;
			this.resourceRepositoryWrite = resourceRepositoryWrite;
		}

		public static decimal GetCostPerLand(decimal currentLand) {
			return Math.Max(1, currentLand / 4);
		}

		// Cumulative cost of colonizing `amount` tiles starting at `currentLand`.
		// Per tile k (0-indexed): max(1, (currentLand + k) / 4). Floor applies to the
		// k < 4 - currentLand prefix; the rest uses a closed-form arithmetic-series sum.
		public static decimal GetCostForTiles(decimal currentLand, int amount) {
			if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), "Amount cannot be negative.");
			if (amount == 0) return 0m;

			int floored = 0;
			if (currentLand < 4m) {
				var threshold = Math.Ceiling(4m - currentLand);
				floored = (int)Math.Min(threshold, amount);
			}

			int remaining = amount - floored;
			if (remaining == 0) return floored;

			// Σ_{k=floored..amount-1} (currentLand + k) / 4
			//   = remaining * (2L + floored + amount - 1) / 8
			var closedForm = remaining * (2m * currentLand + floored + amount - 1) / 8m;
			return floored + closedForm;
		}

		// Largest N such that GetCostForTiles(currentLand, N) <= availableMinerals.
		public static int GetMaxAffordable(decimal currentLand, decimal availableMinerals) {
			if (availableMinerals <= 0) return 0;
			// Each tile costs at least 1, so N <= availableMinerals is a safe upper bound.
			long lo = 0;
			long hi = (long)Math.Min(Math.Floor(availableMinerals) + 1m, int.MaxValue);
			while (lo < hi) {
				long mid = lo + (hi - lo + 1) / 2;
				var cost = GetCostForTiles(currentLand, (int)mid);
				if (cost <= availableMinerals) lo = mid;
				else hi = mid - 1;
			}
			return (int)lo;
		}

		public void Colonize(ColonizeCommand command) {
			if (command.Amount < 1) {
				throw new ArgumentOutOfRangeException(nameof(command.Amount), "Colonization amount must be at least 1.");
			}

			var landId = Id.ResDef("land");
			var mineralsId = Id.ResDef("minerals");
			var currentLand = resourceRepository.GetAmount(command.PlayerId, landId);
			var totalCost = GetCostForTiles(currentLand, command.Amount);

			resourceRepositoryWrite.DeductCost(command.PlayerId, mineralsId, totalCost);
			resourceRepositoryWrite.AddResources(command.PlayerId, landId, command.Amount);
		}
	}
}
