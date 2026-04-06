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

		private readonly object _milestonesLock = new();
		private List<UserMilestoneImmutable> _milestones = new();

		public string? GetUserDisplayName(string userId) {
			var user = Users.Values.FirstOrDefault(u => u.UserId == userId);
			return user?.DisplayName;
		}

		public System.DateTime? GetUserCreated(string userId) {
			var user = Users.Values.FirstOrDefault(u => u.UserId == userId);
			return user?.Created;
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

		public IReadOnlyList<UserMilestoneImmutable> GetAllMilestones() {
			lock (_milestonesLock) return _milestones.ToList();
		}

		public IReadOnlyList<UserMilestoneImmutable> GetMilestonesForUser(string userId) {
			lock (_milestonesLock) return _milestones.Where(m => m.UserId == userId).ToList();
		}

		public bool HasMilestone(string userId, string milestoneId) {
			lock (_milestonesLock) return _milestones.Any(m => m.UserId == userId && m.MilestoneId == milestoneId);
		}

		public void AddMilestone(UserMilestoneImmutable milestone) {
			lock (_milestonesLock) _milestones.Add(milestone);
		}

		public void SetMilestones(IEnumerable<UserMilestoneImmutable> milestones) {
			lock (_milestonesLock) _milestones = milestones.ToList();
		}
	}

	public static class GlobalStateExtensions {
		public static GlobalStateImmutable ToImmutable(this GlobalState globalState) {
			return new GlobalStateImmutable(
				Users: globalState.Users.ToDictionary(x => x.Key, y => y.Value.ToImmutable()),
				Games: globalState.GetGames().ToList(),
				Achievements: globalState.GetAchievements().ToList(),
				Milestones: globalState.GetAllMilestones().ToList()
			);
		}

		public static GlobalState ToMutable(this GlobalStateImmutable globalStateImmutable) {
			var state = new GlobalState {
				Users = new ConcurrentDictionary<string, User>(
					globalStateImmutable.Users.ToDictionary(x => x.Key, y => y.Value.ToMutable()))
			};
			state.SetGames(globalStateImmutable.Games);
			state.SetAchievements(globalStateImmutable.Achievements);
			state.SetMilestones(globalStateImmutable.Milestones ?? Enumerable.Empty<UserMilestoneImmutable>());
			return state;
		}
	}
}
