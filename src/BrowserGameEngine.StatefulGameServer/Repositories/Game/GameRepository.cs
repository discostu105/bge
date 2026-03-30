using BrowserGameEngine.GameModel;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace BrowserGameEngine.StatefulGameServer {
	// Stub game registry. Holds the list of game instances that users can browse and join.
	// This will be replaced by the full multi-game infrastructure in BGE-131.
	public class GameRepository {
		private readonly ConcurrentDictionary<string, GameInfo> games = new();
		private readonly ConcurrentDictionary<string, HashSet<string>> playersByGame = new();

		public GameRepository() {
			SeedDefaultGames();
		}

		public IEnumerable<GameInfo> GetAll() {
			return games.Values.OrderBy(g => g.StartTime);
		}

		public GameInfo? Get(string gameId) {
			games.TryGetValue(gameId, out var game);
			return game;
		}

		public bool IsPlayerInGame(string gameId, string playerId) {
			return playersByGame.TryGetValue(gameId, out var players) && players.Contains(playerId);
		}

		public bool AddPlayer(string gameId, string playerId) {
			var players = playersByGame.GetOrAdd(gameId, _ => new HashSet<string>());
			lock (players) {
				if (!players.Add(playerId)) {
					return false;
				}
				if (games.TryGetValue(gameId, out var game)) {
					game.PlayerCount = players.Count;
				}
			}
			return true;
		}

		private void SeedDefaultGames() {
			var now = DateTime.UtcNow;
			var upcoming1 = new GameInfo {
				GameId = "game-alpha",
				Name = "Alpha Season",
				Status = GameStatus.Upcoming,
				PlayerCount = 0,
				MaxPlayers = 50,
				StartTime = now.AddDays(2),
				EndTime = now.AddDays(16)
			};
			var active1 = new GameInfo {
				GameId = "game-beta",
				Name = "Beta Season",
				Status = GameStatus.Active,
				PlayerCount = 12,
				MaxPlayers = 50,
				StartTime = now.AddDays(-5),
				EndTime = now.AddDays(9)
			};
			var finished1 = new GameInfo {
				GameId = "game-s1",
				Name = "Season 1",
				Status = GameStatus.Finished,
				PlayerCount = 38,
				MaxPlayers = 50,
				StartTime = now.AddDays(-30),
				EndTime = now.AddDays(-16)
			};
			games[upcoming1.GameId] = upcoming1;
			games[active1.GameId] = active1;
			games[finished1.GameId] = finished1;
		}
	}
}
