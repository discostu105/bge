using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.Commands;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Threading;

namespace BrowserGameEngine.StatefulGameServer {
	public class UpgradeRepositoryWrite {
		private const int MaxUpgradeLevel = 3;
		private const int ResearchTimerTicks = 10;

		private readonly IWorldStateAccessor worldStateAccessor;
		private WorldState world => worldStateAccessor.WorldState;
		private readonly ResourceRepository resourceRepository;
		private readonly ResourceRepositoryWrite resourceRepositoryWrite;

		private static readonly Cost[] UpgradeCosts = [
			CostHelper.Create(("minerals", 150), ("gas", 100)),  // level 1
			CostHelper.Create(("minerals", 250), ("gas", 150)),  // level 2
			CostHelper.Create(("minerals", 400), ("gas", 200)),  // level 3
		];

		public UpgradeRepositoryWrite(
			IWorldStateAccessor worldStateAccessor,
			ResourceRepository resourceRepository,
			ResourceRepositoryWrite resourceRepositoryWrite) {
			this.worldStateAccessor = worldStateAccessor;
			this.resourceRepository = resourceRepository;
			this.resourceRepositoryWrite = resourceRepositoryWrite;
		}

		public Cost GetUpgradeCost(int nextLevel) => UpgradeCosts[nextLevel - 1];

		public void ResearchUpgrade(ResearchUpgradeCommand command) {
			var state = world.GetPlayer(command.PlayerId).State;
			lock (state.StateLock) {
				if (state.UpgradeBeingResearched != UpgradeType.None) {
					throw new UpgradeResearchInProgressException(state.UpgradeBeingResearched);
				}

				int currentLevel = command.UpgradeType == UpgradeType.Attack
					? state.AttackUpgradeLevel
					: state.DefenseUpgradeLevel;

				if (currentLevel >= MaxUpgradeLevel) {
					throw new UpgradeAlreadyMaxLevelException(command.UpgradeType);
				}

				var cost = GetUpgradeCost(currentLevel + 1);
				if (!resourceRepository.CanAfford(command.PlayerId, cost)) {
					throw new CannotAffordException(cost);
				}

				resourceRepositoryWrite.DeductCost(command.PlayerId, cost);
				state.UpgradeBeingResearched = command.UpgradeType;
				state.UpgradeResearchTimer = ResearchTimerTicks;
			}
		}

		public void ProcessResearchTimer(PlayerId playerId) {
			var state = world.GetPlayer(playerId).State;
			lock (state.StateLock) {
				if (state.UpgradeBeingResearched == UpgradeType.None || state.UpgradeResearchTimer <= 0) {
					return;
				}

				state.UpgradeResearchTimer--;
				if (state.UpgradeResearchTimer == 0) {
					if (state.UpgradeBeingResearched == UpgradeType.Attack) {
						state.AttackUpgradeLevel++;
					} else if (state.UpgradeBeingResearched == UpgradeType.Defense) {
						state.DefenseUpgradeLevel++;
					}
					state.UpgradeBeingResearched = UpgradeType.None;
				}
			}
		}
	}
}
