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

		public void Colonize(ColonizeCommand command) {
			if (command.Amount < 1 || command.Amount > 24) {
				throw new ArgumentOutOfRangeException(nameof(command.Amount), "Colonization amount must be between 1 and 24.");
			}

			var landId = Id.ResDef("land");
			var mineralsId = Id.ResDef("minerals");
			var currentLand = resourceRepository.GetAmount(command.PlayerId, landId);
			var costPerLand = GetCostPerLand(currentLand);
			var totalCost = command.Amount * costPerLand;

			resourceRepositoryWrite.DeductCost(command.PlayerId, mineralsId, totalCost);
			resourceRepositoryWrite.AddResources(command.PlayerId, landId, command.Amount);
		}
	}
}
