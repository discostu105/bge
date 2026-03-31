using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;

namespace BrowserGameEngine.StatefulGameServer.GameTicks.Modules {
	public class TechResearchTimerModule : IGameTickModule {
		public string Name => "techresearch:1";
		private readonly TechRepositoryWrite techRepositoryWrite;

		public TechResearchTimerModule(TechRepositoryWrite techRepositoryWrite) {
			this.techRepositoryWrite = techRepositoryWrite;
		}

		public void SetProperty(string name, string value) {
			switch (name) {
				default:
					throw new InvalidGameDefException($"Property '{name}' not valid for GameTickModule '{this.Name}'.");
			}
		}

		public void CalculateTick(PlayerId playerId) {
			techRepositoryWrite.ProcessResearchTimer(playerId);
		}
	}
}
