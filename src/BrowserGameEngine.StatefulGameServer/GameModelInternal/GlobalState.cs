using BrowserGameEngine.GameModel;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace BrowserGameEngine.StatefulGameServer.GameModelInternal {
	public class GlobalState {
		internal ConcurrentDictionary<string, User> Users { get; set; } = new();

		private readonly object _gamesLock = new();
		private List<GameRecordImmutable> _games = new();

		private readonly object _tournamentsLock = new();
		private List<TournamentImmutable> _tournaments = new();

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

		public IReadOnlyList<TournamentImmutable> GetTournaments() {
			lock (_tournamentsLock) return _tournaments.ToList();
		}

		public TournamentImmutable? GetTournamentById(string tournamentId) {
			lock (_tournamentsLock) return _tournaments.FirstOrDefault(t => t.TournamentId == tournamentId);
		}

		public void AddTournament(TournamentImmutable tournament) {
			lock (_tournamentsLock) _tournaments.Add(tournament);
		}

		public void UpdateTournament(TournamentImmutable old, TournamentImmutable updated) {
			lock (_tournamentsLock) {
				var idx = _tournaments.IndexOf(old);
				if (idx >= 0) _tournaments[idx] = updated;
			}
		}

		public void SetTournaments(IEnumerable<TournamentImmutable> tournaments) {
			lock (_tournamentsLock) _tournaments = tournaments.ToList();
		}
	}

	public static class GlobalStateExtensions {
		public static GlobalStateImmutable ToImmutable(this GlobalState globalState) {
			return new GlobalStateImmutable(
				Users: globalState.Users.ToDictionary(x => x.Key, y => y.Value.ToImmutable()),
				Games: globalState.GetGames().ToList(),
				Tournaments: globalState.GetTournaments().ToList()
			);
		}

		public static GlobalState ToMutable(this GlobalStateImmutable globalStateImmutable) {
			var state = new GlobalState {
				Users = new ConcurrentDictionary<string, User>(
					globalStateImmutable.Users.ToDictionary(x => x.Key, y => y.Value.ToMutable()))
			};
			state.SetGames(globalStateImmutable.Games);
			state.SetTournaments(globalStateImmutable.Tournaments ?? Enumerable.Empty<TournamentImmutable>());
			return state;
		}
	}
}
