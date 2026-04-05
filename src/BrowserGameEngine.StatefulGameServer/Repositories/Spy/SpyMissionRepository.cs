using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using System.Collections.Generic;
using System.Linq;

namespace BrowserGameEngine.StatefulGameServer {
	public class SpyMissionRepository {
		private readonly IWorldStateAccessor worldStateAccessor;
		private WorldState world => worldStateAccessor.WorldState;

		public SpyMissionRepository(IWorldStateAccessor worldStateAccessor) {
			this.worldStateAccessor = worldStateAccessor;
		}

		public IReadOnlyList<SpyMissionImmutable> GetMissions(PlayerId spyingPlayerId) {
			return world.GetPlayer(spyingPlayerId).State.SpyMissions
				.OrderByDescending(m => m.CreatedAt)
				.Select(m => m.ToImmutable())
				.ToList();
		}

		public IReadOnlyList<SpyMissionImmutable> GetActiveMissions(PlayerId spyingPlayerId) {
			return world.GetPlayer(spyingPlayerId).State.SpyMissions
				.Where(m => m.Status == SpyMissionStatus.InTransit)
				.Select(m => m.ToImmutable())
				.ToList();
		}
	}
}
