using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using System.Threading;

namespace BrowserGameEngine.StatefulGameServer {
	public class BattleReportRepositoryWrite {
		internal const int MaxReportsPerPlayer = 50;
		private readonly Lock _lock = new();
		private readonly IWorldStateAccessor worldStateAccessor;
		private WorldState world => worldStateAccessor.WorldState;

		public BattleReportRepositoryWrite(IWorldStateAccessor worldStateAccessor) {
			this.worldStateAccessor = worldStateAccessor;
		}

		internal void AddBattleReport(PlayerId playerId, BattleReport report) {
			lock (_lock) {
				var reports = world.GetPlayer(playerId).State.BattleReports;
				reports.Add(report);
				while (reports.Count > MaxReportsPerPlayer) {
					reports.RemoveAt(0);
				}
			}
		}
	}
}
