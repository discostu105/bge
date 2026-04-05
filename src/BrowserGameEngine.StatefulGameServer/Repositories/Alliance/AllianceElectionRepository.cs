using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using System.Linq;

namespace BrowserGameEngine.StatefulGameServer {
	public class AllianceElectionRepository {
		private readonly IWorldStateAccessor worldStateAccessor;
		private WorldState world => worldStateAccessor.WorldState;

		public AllianceElectionRepository(IWorldStateAccessor worldStateAccessor) {
			this.worldStateAccessor = worldStateAccessor;
		}

		public AllianceElectionImmutable? GetActiveElection(AllianceId allianceId) {
			if (!world.Alliances.TryGetValue(allianceId, out var alliance)) return null;
			return alliance.ActiveElection?.ToImmutable();
		}

		public AllianceElectionImmutable? GetElection(AllianceElectionId electionId) {
			foreach (var alliance in world.Alliances.Values) {
				if (alliance.ActiveElection?.ElectionId == electionId) {
					return alliance.ActiveElection.ToImmutable();
				}
				var historical = alliance.ElectionHistory.FirstOrDefault(e => e.ElectionId == electionId);
				if (historical != null) {
					return historical.ToImmutable();
				}
			}
			return null;
		}

		public System.Collections.Generic.IEnumerable<AllianceElectionImmutable> GetElectionHistory(AllianceId allianceId) {
			if (!world.Alliances.TryGetValue(allianceId, out var alliance)) {
				return System.Linq.Enumerable.Empty<AllianceElectionImmutable>();
			}
			return alliance.ElectionHistory.Select(e => e.ToImmutable());
		}
	}
}
