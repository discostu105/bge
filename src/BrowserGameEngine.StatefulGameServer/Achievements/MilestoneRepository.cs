using System;
using System.Collections.Generic;
using System.Linq;
using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using BrowserGameEngine.StatefulGameServer.GameRegistry;

namespace BrowserGameEngine.StatefulGameServer.Achievements {
	public record MilestoneEvaluation(
		MilestoneDefinition Definition,
		bool IsUnlocked,
		DateTime? UnlockedAt,
		int CurrentProgress
	);

	public class MilestoneRepository {
		private readonly GlobalState _globalState;
		private readonly GameRegistry.GameRegistry _gameRegistry;

		public MilestoneRepository(GlobalState globalState, GameRegistry.GameRegistry gameRegistry) {
			_globalState = globalState;
			_gameRegistry = gameRegistry;
		}

		public IReadOnlyList<MilestoneEvaluation> GetMilestonesForUser(string userId) {
			var achievements = _globalState.GetAchievements().Where(a => a.UserId == userId).ToList();
			var unlockedMilestones = _globalState.GetMilestonesForUser(userId);

			var activeInstance = _gameRegistry.GetAllInstances()
				.FirstOrDefault(i => i.HasUserPlayer(userId));
			var playerState = activeInstance != null
				? GetPlayerState(activeInstance, userId)
				: null;

			var results = new List<MilestoneEvaluation>();
			foreach (var def in MilestoneCatalogue.All) {
				var unlocked = unlockedMilestones.FirstOrDefault(m => m.MilestoneId == def.Id);
				var current = ComputeProgress(def, achievements, playerState, activeInstance, userId);

				results.Add(new MilestoneEvaluation(
					Definition: def,
					IsUnlocked: unlocked != null,
					UnlockedAt: unlocked?.UnlockedAt,
					CurrentProgress: current
				));
			}
			return results;
		}

		private static PlayerState? GetPlayerState(GameInstance instance, string userId) {
			var pair = instance.TryGetUserPlayer(userId);
			if (pair == null) return null;
			instance.WorldState.Players.TryGetValue(pair.Value.PlayerId, out var player);
			return player?.State;
		}

		private static decimal GetResource(PlayerState state, string resourceId) {
			state.Resources.TryGetValue(Id.ResDef(resourceId), out var value);
			return value;
		}

		private int ComputeProgress(
			MilestoneDefinition def,
			IList<PlayerAchievementImmutable> achievements,
			PlayerState? playerState,
			GameInstance? activeInstance,
			string userId) {

			return def.Id switch {
				"games-first"     => Math.Min(achievements.Count, def.TargetProgress),
				"games-veteran"   => Math.Min(achievements.Count, def.TargetProgress),
				"games-commander" => Math.Min(achievements.Count, def.TargetProgress),
				"games-legend"    => Math.Min(achievements.Count, def.TargetProgress),
				"games-100"       => Math.Min(achievements.Count, def.TargetProgress),

				"win-first"    => Math.Min(achievements.Count(a => a.FinalRank == 1), def.TargetProgress),
				"win-champion" => Math.Min(achievements.Count(a => a.FinalRank == 1), def.TargetProgress),
				"win-legend"   => Math.Min(achievements.Count(a => a.FinalRank == 1), def.TargetProgress),
				"top3-first"   => Math.Min(achievements.Count(a => a.FinalRank <= 3), def.TargetProgress),

				"win-streak-5"      => Math.Min(GetCurrentWinStreak(achievements), def.TargetProgress),
				"top3-leaderboard"  => GetLeaderboardRank(userId) <= 3 ? 1 : 0,
				"difficulty-master" => HasWonLargeGame(achievements) ? 1 : 0,

				"econ-minerals" => playerState == null ? 0 : Math.Min((int)GetResource(playerState, "minerals"), def.TargetProgress),
				"econ-gas"      => playerState == null ? 0 : Math.Min((int)GetResource(playerState, "gas"),      def.TargetProgress),
				"econ-land-100" => playerState == null ? 0 : Math.Min((int)GetResource(playerState, "land"),     def.TargetProgress),
				"econ-land-500" => playerState == null ? 0 : Math.Min((int)GetResource(playerState, "land"),     def.TargetProgress),

				"diplo-alliance" => activeInstance == null ? 0 : GetAllianceProgress(activeInstance, userId),
				"market-first"   => activeInstance == null ? 0 : GetMarketProgress(activeInstance, userId),

				"upgrade-first" => playerState == null ? 0 : Math.Min(playerState.UnlockedTechs.Count, def.TargetProgress),

				_ => 0
			};
		}

		private static int GetCurrentWinStreak(IList<PlayerAchievementImmutable> achievements) {
			var sorted = achievements.OrderBy(a => a.FinishedAt).ToList();
			int streak = 0;
			for (int i = sorted.Count - 1; i >= 0; i--) {
				if (sorted[i].FinalRank == 1) {
					streak++;
				} else {
					break;
				}
			}
			return streak;
		}

		private int GetLeaderboardRank(string userId) {
			var allAchievements = _globalState.GetAchievements();
			var rankedUsers = allAchievements
				.GroupBy(a => a.UserId)
				.Select(g => (UserId: g.Key, Wins: g.Count(a => a.FinalRank == 1)))
				.OrderByDescending(x => x.Wins)
				.ToList();
			var idx = rankedUsers.FindIndex(x => x.UserId == userId);
			return idx == -1 ? int.MaxValue : idx + 1;
		}

		private bool HasWonLargeGame(IList<PlayerAchievementImmutable> achievements) {
			var winningGameIds = achievements
				.Where(a => a.FinalRank == 1)
				.Select(a => a.GameId.Id)
				.ToHashSet();
			if (winningGameIds.Count == 0) return false;
			var allAchievements = _globalState.GetAchievements();
			foreach (var gameId in winningGameIds) {
				var playerCount = allAchievements.Count(a => a.GameId.Id == gameId);
				if (playerCount >= 5) return true;
			}
			return false;
		}

		private static int GetAllianceProgress(GameInstance instance, string userId) {
			var pair = instance.TryGetUserPlayer(userId);
			if (pair == null) return 0;
			if (!instance.WorldState.Players.TryGetValue(pair.Value.PlayerId, out var player)) return 0;
			return player.AllianceId != null ? 1 : 0;
		}

		private static int GetMarketProgress(GameInstance instance, string userId) {
			var pair = instance.TryGetUserPlayer(userId);
			if (pair == null) return 0;
			var playerId = pair.Value.PlayerId;
			lock (instance.WorldState.MarketOrdersLock) {
				return instance.WorldState.MarketOrders
					.Any(o => o.SellerPlayerId == playerId && o.Status == MarketOrderStatus.Filled)
					? 1 : 0;
			}
		}
	}
}
