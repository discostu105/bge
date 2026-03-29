using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BrowserGameEngine.StatefulGameServer {
	public class BuildQueueRepository {
		private readonly WorldState world;

		public BuildQueueRepository(WorldState world) {
			this.world = world;
		}

		private List<BuildQueueEntry> Queue(PlayerId playerId) => world.GetPlayer(playerId).State.BuildQueue;

		public IList<BuildQueueEntryImmutable> GetQueue(PlayerId playerId) {
			return Queue(playerId).OrderBy(x => x.Priority).Select(x => x.ToImmutable()).ToList();
		}

		internal BuildQueueEntry? GetEntryMutable(PlayerId playerId, Guid entryId) {
			return Queue(playerId).SingleOrDefault(x => x.Id == entryId);
		}

		internal int GetNextPriority(PlayerId playerId) {
			var queue = Queue(playerId);
			if (!queue.Any()) return 0;
			return queue.Max(x => x.Priority) + 1;
		}
	}
}
