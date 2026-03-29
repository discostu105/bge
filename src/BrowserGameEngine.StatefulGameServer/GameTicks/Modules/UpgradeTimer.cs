using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using System;

namespace BrowserGameEngine.StatefulGameServer.GameTicks.Modules {
	public class UpgradeTimer : IGameTickModule {
		public string Name => "upgradetimer:1";
		private readonly UpgradeRepositoryWrite upgradeRepositoryWrite;

		public UpgradeTimer(UpgradeRepositoryWrite upgradeRepositoryWrite) {
			this.upgradeRepositoryWrite = upgradeRepositoryWrite;
		}

		public void SetProperty(string name, string value) {
			switch (name) {
				default:
					throw new InvalidGameDefException($"Property '{name}' not valid for GameTickModule '{this.Name}'.");
			}
		}

		public void CalculateTick(PlayerId playerId) {
			upgradeRepositoryWrite.ProcessResearchTimer(playerId);
		}
	}
}
