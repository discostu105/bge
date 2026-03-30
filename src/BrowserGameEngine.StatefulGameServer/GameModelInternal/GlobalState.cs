using BrowserGameEngine.GameModel;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace BrowserGameEngine.StatefulGameServer.GameModelInternal {
	public class GlobalState {
		internal ConcurrentDictionary<string, User> Users { get; set; } = new();
		public IList<GameRecordImmutable> Games { get; set; } = new List<GameRecordImmutable>();
		public IList<PlayerAchievementImmutable> Achievements { get; set; } = new List<PlayerAchievementImmutable>();
	}

	public static class GlobalStateExtensions {
		public static GlobalStateImmutable ToImmutable(this GlobalState globalState) {
			return new GlobalStateImmutable(
				Users: globalState.Users.ToDictionary(x => x.Key, y => y.Value.ToImmutable()),
				Games: globalState.Games.ToList(),
				Achievements: globalState.Achievements.ToList()
			);
		}

		public static GlobalState ToMutable(this GlobalStateImmutable globalStateImmutable) {
			return new GlobalState {
				Users = new ConcurrentDictionary<string, User>(
					globalStateImmutable.Users.ToDictionary(x => x.Key, y => y.Value.ToMutable())),
				Games = globalStateImmutable.Games.ToList(),
				Achievements = globalStateImmutable.Achievements.ToList()
			};
		}
	}
}
