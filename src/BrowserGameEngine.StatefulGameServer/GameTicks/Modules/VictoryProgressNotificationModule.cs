using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using BrowserGameEngine.StatefulGameServer.Notifications;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace BrowserGameEngine.StatefulGameServer.GameTicks.Modules {
	public class VictoryProgressNotificationModule : IGameTickModule {
		public string Name => "victoryprogress:1";

		private static readonly int[] Milestones = { 50, 75, 90 };

		private readonly IWorldStateAccessor worldStateAccessor;
		private readonly GameDef gameDef;
		private readonly IPlayerNotificationService notificationService;
		private readonly ConcurrentDictionary<string, HashSet<int>> _reachedMilestones = new();

		private decimal _threshold = 500_000;

		public VictoryProgressNotificationModule(
			IWorldStateAccessor worldStateAccessor,
			GameDef gameDef,
			IPlayerNotificationService notificationService) {
			this.worldStateAccessor = worldStateAccessor;
			this.gameDef = gameDef;
			this.notificationService = notificationService;
		}

		public void SetProperty(string name, string value) {
			if (name == "threshold" && decimal.TryParse(value, out var t)) _threshold = t;
		}

		public void CalculateTick(PlayerId playerId) {
			if (_threshold <= 0) return;

			var score = GetScore(playerId);
			var percent = (int)(score / _threshold * 100);
			var reached = _reachedMilestones.GetOrAdd(playerId.Id, _ => new HashSet<int>());

			foreach (var milestone in Milestones) {
				if (percent >= milestone && reached.Add(milestone)) {
					var playerName = GetPlayerName(playerId);
					NotifyAllPlayers(playerName, milestone);
				}
			}
		}

		private void NotifyAllPlayers(string playerName, int milestone) {
			var message = $"{playerName} has reached {milestone}% of the victory threshold!";
			foreach (var player in worldStateAccessor.WorldState.Players) {
				if (player.Value.UserId != null) {
					notificationService.Push(player.Value.UserId, message, NotificationKind.GameEvent);
				}
			}
		}

		private string GetPlayerName(PlayerId playerId) {
			if (worldStateAccessor.WorldState.Players.TryGetValue(playerId, out var player)) {
				return player.Name;
			}
			return "Unknown";
		}

		private decimal GetScore(PlayerId playerId) {
			if (worldStateAccessor.WorldState.Players.TryGetValue(playerId, out var player)) {
				if (player.State.Resources.TryGetValue(gameDef.ScoreResource, out var score)) {
					return score;
				}
			}
			return 0;
		}
	}
}
