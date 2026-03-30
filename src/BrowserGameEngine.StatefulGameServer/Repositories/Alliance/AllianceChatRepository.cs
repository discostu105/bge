using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using System.Collections.Generic;
using System.Linq;

namespace BrowserGameEngine.StatefulGameServer {
	public class AllianceChatRepository {
		private readonly IWorldStateAccessor worldStateAccessor;
		private WorldState world => worldStateAccessor.WorldState;

		public AllianceChatRepository(IWorldStateAccessor worldStateAccessor) {
			this.worldStateAccessor = worldStateAccessor;
		}

		public IList<AlliancePostImmutable> GetPosts(AllianceId allianceId, int count = 50) {
			if (!world.Alliances.TryGetValue(allianceId, out var alliance)) return new List<AlliancePostImmutable>();
			return alliance.Posts
				.Select(p => p.ToImmutable())
				.OrderByDescending(p => p.CreatedAt)
				.Take(count)
				.ToList();
		}
	}
}
