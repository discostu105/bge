using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;

namespace BrowserGameEngine.StatefulGameServer {
	public class BattleReportRepositoryWrite {
		public const int MaxReportsPerPlayer = 50;
		private readonly IWorldStateAccessor worldStateAccessor;
		private WorldState world => worldStateAccessor.WorldState;

		public BattleReportRepositoryWrite(IWorldStateAccessor worldStateAccessor) {
			this.worldStateAccessor = worldStateAccessor;
		}

		public void AddBattleReport(PlayerId playerId, BattleReport report) {
			var state = world.GetPlayer(playerId).State;
			lock (state.StateLock) {
				state.BattleReports.Add(report);
				while (state.BattleReports.Count > MaxReportsPerPlayer) {
					state.BattleReports.RemoveAt(0);
				}
			}
		}
	}
}
