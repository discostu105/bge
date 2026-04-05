using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;

namespace BrowserGameEngine.StatefulGameServer.GameTicks.Modules {
	public class SpyMissionModule : IGameTickModule {
		public string Name => "spymission:1";
		private readonly SpyMissionRepositoryWrite spyMissionRepositoryWrite;

		public SpyMissionModule(SpyMissionRepositoryWrite spyMissionRepositoryWrite) {
			this.spyMissionRepositoryWrite = spyMissionRepositoryWrite;
		}

		public void SetProperty(string name, string value) {
			switch (name) {
				default:
					throw new InvalidGameDefException($"Property '{name}' not valid for GameTickModule '{this.Name}'.");
			}
		}

		public void CalculateTick(PlayerId playerId) {
			spyMissionRepositoryWrite.ProcessMissions(playerId);
		}
	}
}
