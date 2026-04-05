using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BrowserGameEngine.StatefulGameServer {
	public class BattleReportRepository {
		private readonly IWorldStateAccessor worldStateAccessor;
		private WorldState world => worldStateAccessor.WorldState;

		public BattleReportRepository(IWorldStateAccessor worldStateAccessor) {
			this.worldStateAccessor = worldStateAccessor;
		}

		public BattleReport? GetBattleReport(PlayerId playerId, Guid reportId) {
			return world.GetPlayer(playerId).State.BattleReports
				.FirstOrDefault(r => r.Id == reportId);
		}

		public List<BattleReport> GetBattleReports(PlayerId playerId) {
			return world.GetPlayer(playerId).State.BattleReports;
		}
	}
}
