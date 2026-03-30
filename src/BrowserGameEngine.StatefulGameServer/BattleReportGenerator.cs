using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.Notifications;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BrowserGameEngine.StatefulGameServer {
	public class BattleReportGenerator {
		private readonly PlayerRepository playerRepository;
		private readonly MessageRepositoryWrite messageRepositoryWrite;
		private readonly GameDef gameDef;
		private readonly IPlayerNotificationService playerNotificationService;

		public BattleReportGenerator(
			PlayerRepository playerRepository,
			MessageRepositoryWrite messageRepositoryWrite,
			GameDef gameDef,
			IPlayerNotificationService playerNotificationService
		) {
			this.playerRepository = playerRepository;
			this.messageRepositoryWrite = messageRepositoryWrite;
			this.gameDef = gameDef;
			this.playerNotificationService = playerNotificationService;
		}

		public void GenerateReports(BattleResult battleResult) {
			var attacker = playerRepository.Get(battleResult.Attacker);
			var defender = playerRepository.Get(battleResult.Defender);

			var attackerRace = GetRaceName(attacker.PlayerType);
			var defenderRace = GetRaceName(defender.PlayerType);

			bool attackerWon = !battleResult.BtlResult.DefendingUnitsSurvived.Any()
				&& battleResult.BtlResult.AttackingUnitsSurvived.Any();
			bool draw = !battleResult.BtlResult.AttackingUnitsSurvived.Any() && !battleResult.BtlResult.DefendingUnitsSurvived.Any();
			string outcome = attackerWon ? "Attacker won" : draw ? "Draw" : "Defender won";

			var resourcesStolen = battleResult.BtlResult.ResourcesStolen
				.SelectMany(c => c.Resources)
				.GroupBy(x => x.Key.Id)
				.ToDictionary(g => g.Key, g => g.Sum(x => x.Value));

			string body = BuildBody(
				attacker.Name, attackerRace,
				defender.Name, defenderRace,
				outcome,
				(int)battleResult.BtlResult.LandTransferred,
				battleResult.BtlResult.WorkersCaptured,
				resourcesStolen,
				battleResult.BtlResult.AttackingUnitsDestroyed,
				battleResult.BtlResult.DefendingUnitsDestroyed
			);

			messageRepositoryWrite.SendMessage(
				battleResult.Attacker,
				$"Battle Report vs {defender.Name}",
				body
			);
			messageRepositoryWrite.SendMessage(
				battleResult.Defender,
				$"Battle Report vs {attacker.Name}",
				body
			);

			if (defender.UserId != null) {
				playerNotificationService.Push(defender.UserId, $"Your base was attacked by {attacker.Name}! ({outcome})", NotificationKind.Warning);
			}
			if (attacker.UserId != null) {
				string pillageNote = resourcesStolen.Count > 0
					? $" Pillaged: {string.Join(", ", resourcesStolen.Select(kv => $"{kv.Value} {kv.Key}"))}"
					: string.Empty;
				playerNotificationService.Push(attacker.UserId, $"Battle vs {defender.Name}: {outcome}.{pillageNote}", attackerWon ? NotificationKind.Info : NotificationKind.Warning);
			}
		}

		private string GetRaceName(PlayerTypeDefId playerTypeDefId) {
			return gameDef.PlayerTypes.SingleOrDefault(x => x.Id.Equals(playerTypeDefId))?.Name ?? playerTypeDefId.Id;
		}

		private string BuildBody(
			string attackerName, string attackerRace,
			string defenderName, string defenderRace,
			string outcome,
			int landTransferred,
			int workersCaptured,
			Dictionary<string, decimal> resourcesStolen,
			List<UnitCount> attackerLosses,
			List<UnitCount> defenderLosses
		) {
			var sb = new StringBuilder();
			sb.AppendLine($"Attacker: {attackerName} ({attackerRace})");
			sb.AppendLine($"Defender: {defenderName} ({defenderRace})");
			sb.AppendLine($"Outcome: {outcome}");
			sb.AppendLine($"Land transferred: {landTransferred}");
			if (workersCaptured > 0) sb.AppendLine($"Workers captured: {workersCaptured}");
			if (resourcesStolen.Count > 0) {
				var stolen = string.Join(", ", resourcesStolen.Select(kv => $"{kv.Value} {kv.Key}"));
				sb.AppendLine($"Resources pillaged: {stolen}");
			}
			sb.AppendLine($"Attacker losses: {FormatUnitBreakdown(attackerLosses)}");
			sb.AppendLine($"Defender losses: {FormatUnitBreakdown(defenderLosses)}");
			return sb.ToString();
		}

		private string FormatUnitBreakdown(List<UnitCount> unitCounts) {
			if (!unitCounts.Any()) return "none";
			return string.Join(", ", unitCounts.Select(uc => {
				var unitDef = gameDef.GetUnitDef(uc.UnitDefId);
				var name = unitDef?.Name ?? uc.UnitDefId.Id;
				return $"{uc.Count}x {name}";
			}));
		}
	}
}
