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

			if (defender.UserId != null) {
				playerNotificationService.Push(defender.UserId, $"Your base was attacked by {attacker.Name}!", NotificationKind.Warning);
			}
			var attackerRace = GetRaceName(attacker.PlayerType);
			var defenderRace = GetRaceName(defender.PlayerType);

			bool attackerWon = !battleResult.BtlResult.DefendingUnitsSurvived.Any()
				&& battleResult.BtlResult.AttackingUnitsSurvived.Any();
			string outcome = attackerWon ? "Attacker won" : "Defender won";

			string body = BuildBody(
				attacker.Name, attackerRace,
				defender.Name, defenderRace,
				outcome,
				(int)battleResult.BtlResult.LandTransferred,
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
		}

		private string GetRaceName(PlayerTypeDefId playerTypeDefId) {
			return gameDef.PlayerTypes.SingleOrDefault(x => x.Id.Equals(playerTypeDefId))?.Name ?? playerTypeDefId.Id;
		}

		private string BuildBody(
			string attackerName, string attackerRace,
			string defenderName, string defenderRace,
			string outcome,
			int landTransferred,
			List<UnitCount> attackerLosses,
			List<UnitCount> defenderLosses
		) {
			var sb = new StringBuilder();
			sb.AppendLine($"Attacker: {attackerName} ({attackerRace})");
			sb.AppendLine($"Defender: {defenderName} ({defenderRace})");
			sb.AppendLine($"Outcome: {outcome}");
			sb.AppendLine($"Land transferred: {landTransferred}");
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
