using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrowserGameEngine.StatefulGameServer.GameTicks.Modules {
	public class ActionQueueExecutor : IGameTickModule {
		public string Name => "actionqueue:1";

		public void SetProperty(string name, string value) { }

		private readonly AssetRepositoryWrite assetRepositoryWrite;

		public ActionQueueExecutor(AssetRepositoryWrite assetRepositoryWrite) {
			this.assetRepositoryWrite = assetRepositoryWrite;
		}

		public void CalculateTick(PlayerId playerId) {
			assetRepositoryWrite.ExecuteGameActions(playerId);
		}
	}
}
