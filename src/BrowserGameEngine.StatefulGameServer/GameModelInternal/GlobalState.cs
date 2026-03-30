using BrowserGameEngine.GameModel;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace BrowserGameEngine.StatefulGameServer.GameModelInternal {
	public class GlobalState {
		internal ConcurrentDictionary<string, User> Users { get; set; } = new();

		private readonly object _gamesLock = new();
		private List<GameRecordImmutable> _games = new();

		private readonly object _achievementsLock = new();
		private List<PlayerAchievementImmutable> _achievements = new();

		public string? GetUserDisplayName(string userId) {
			var user = Users.Values.FirstOrDefault(u => u.UserId == userId);
			return user?.DisplayName;
		}

		public IReadOnlyList<GameRecordImmutable> GetGames() {
			lock (_gamesLock) return _games.ToList();
		}

		public void AddGame(GameRecordImmutable record) {
			lock (_gamesLock) _games.Add(record);
		}

		public void UpdateGame(GameRecordImmutable old, GameRecordImmutable updated) {
			lock (_gamesLock) {
				var idx = _games.IndexOf(old);
				if (idx >= 0) _games[idx] = updated;
			}
		}

		public void SetGames(IEnumerable<GameRecordImmutable> games) {
			lock (_gamesLock) _games = games.ToList();
		}

		public IReadOnlyList<PlayerAchievementImmutable> GetAchievements() {
			lock (_achievementsLock) return _achievements.ToList();
		}

		public void AddAchievement(PlayerAchievementImmutable achievement) {
			lock (_achievementsLock) _achievements.Add(achievement);
		}

		public void SetAchievements(IEnumerable<PlayerAchievementImmutable> achievements) {
			lock (_achievementsLock) _achievements = achievements.ToList();
		}
	}

	public static class GlobalStateExtensions {
		public static GlobalStateImmutable ToImmutable(this GlobalState globalState) {
			return new GlobalStateImmutable(
				Users: globalState.Users.ToDictionary(x => x.Key, y => y.Value.ToImmutable()),
				Games: globalState.GetGames().ToList(),
				Achievements: globalState.GetAchievements().ToList()
			);
		}

		public static GlobalState ToMutable(this GlobalStateImmutable globalStateImmutable) {
			var state = new GlobalState {
				Users = new ConcurrentDictionary<string, User>(
					globalStateImmutable.Users.ToDictionary(x => x.Key, y => y.Value.ToMutable()))
			};
			state.SetGames(globalStateImmutable.Games);
			state.SetAchievements(globalStateImmutable.Achievements);
			return state;
		}
	}
}
