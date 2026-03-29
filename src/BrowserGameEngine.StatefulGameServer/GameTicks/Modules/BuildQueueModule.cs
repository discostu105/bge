using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.GameTicks;

namespace BrowserGameEngine.StatefulGameServer.GameTicks.Modules {
	public class BuildQueueModule : IGameTickModule {
		public string Name => "buildqueue:1";

		public void SetProperty(string name, string value) { }

		private readonly BuildQueueRepositoryWrite buildQueueRepositoryWrite;

		public BuildQueueModule(BuildQueueRepositoryWrite buildQueueRepositoryWrite) {
			this.buildQueueRepositoryWrite = buildQueueRepositoryWrite;
		}

		public void CalculateTick(PlayerId playerId) {
			buildQueueRepositoryWrite.TryExecuteAndDequeueFirst(playerId);
		}
	}
}
