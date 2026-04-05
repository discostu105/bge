using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using BrowserGameEngine.StatefulGameServer.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BrowserGameEngine.StatefulGameServer {
	public class BattleReportGenerator {
		private readonly PlayerRepository playerRepository;
		private readonly MessageRepositoryWrite messageRepositoryWrite;
		private readonly BattleReportRepositoryWrite battleReportRepositoryWrite;
		private readonly GameDef gameDef;
		private readonly IPlayerNotificationService playerNotificationService;
		private readonly INotificationService notificationService;
		private readonly TimeProvider timeProvider;

		public BattleReportGenerator(
			PlayerRepository playerRepository,
			MessageRepositoryWrite messageRepositoryWrite,
			BattleReportRepositoryWrite battleReportRepositoryWrite,
			GameDef gameDef,
			IPlayerNotificationService playerNotificationService,
			INotificationService notificationService,
			TimeProvider timeProvider
		) {
			this.playerRepository = playerRepository;
			this.messageRepositoryWrite = messageRepositoryWrite;
			this.battleReportRepositoryWrite = battleReportRepositoryWrite;
			this.gameDef = gameDef;
			this.playerNotificationService = playerNotificationService;
			this.notificationService = notificationService;
			this.timeProvider = timeProvider;
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

			// Persist structured battle report
			var reportId = Guid.NewGuid();
			var initialAttackerUnits = battleResult.BtlResult.Rounds.Count > 0
				? battleResult.BtlResult.Rounds[0].AttackerUnitsRemaining
					.Concat(battleResult.BtlResult.Rounds[0].AttackerCasualties)
					.GroupBy(u => u.UnitDefId)
					.Select(g => new UnitCount(g.Key, g.Sum(x => x.Count)))
					.ToList()
				: battleResult.BtlResult.AttackingUnitsSurvived
					.Concat(battleResult.BtlResult.AttackingUnitsDestroyed)
					.GroupBy(u => u.UnitDefId)
					.Select(g => new UnitCount(g.Key, g.Sum(x => x.Count)))
					.ToList();
			var initialDefenderUnits = battleResult.BtlResult.Rounds.Count > 0
				? battleResult.BtlResult.Rounds[0].DefenderUnitsRemaining
					.Concat(battleResult.BtlResult.Rounds[0].DefenderCasualties)
					.GroupBy(u => u.UnitDefId)
					.Select(g => new UnitCount(g.Key, g.Sum(x => x.Count)))
					.ToList()
				: battleResult.BtlResult.DefendingUnitsSurvived
					.Concat(battleResult.BtlResult.DefendingUnitsDestroyed)
					.GroupBy(u => u.UnitDefId)
					.Select(g => new UnitCount(g.Key, g.Sum(x => x.Count)))
					.ToList();

			var report = new BattleReport {
				Id = reportId,
				AttackerId = battleResult.Attacker,
				DefenderId = battleResult.Defender,
				AttackerName = attacker.Name,
				DefenderName = defender.Name,
				AttackerRace = attackerRace,
				DefenderRace = defenderRace,
				Outcome = outcome,
				TotalAttackerStrengthBefore = battleResult.BtlResult.TotalAttackerStrengthBefore,
				TotalDefenderStrengthBefore = battleResult.BtlResult.TotalDefenderStrengthBefore,
				AttackerUnitsInitial = initialAttackerUnits,
				DefenderUnitsInitial = initialDefenderUnits,
				Rounds = new List<BattleRoundSnapshotImmutable>(battleResult.BtlResult.Rounds),
				LandTransferred = (int)battleResult.BtlResult.LandTransferred,
				WorkersCaptured = battleResult.BtlResult.WorkersCaptured,
				ResourcesStolen = resourcesStolen,
				CreatedAt = timeProvider.GetUtcNow().UtcDateTime
			};

			battleReportRepositoryWrite.AddBattleReport(battleResult.Attacker, report);
			battleReportRepositoryWrite.AddBattleReport(battleResult.Defender, report);

			string body = BuildBody(
				attacker.Name, attackerRace,
				defender.Name, defenderRace,
				outcome,
				(int)battleResult.BtlResult.LandTransferred,
				battleResult.BtlResult.WorkersCaptured,
				resourcesStolen,
				battleResult.BtlResult.AttackingUnitsDestroyed,
				battleResult.BtlResult.DefendingUnitsDestroyed,
				reportId
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

			notificationService.Notify(battleResult.Defender, GameNotificationType.AttackReceived,
				$"Attack from {attacker.Name}",
				$"Outcome: {outcome}");

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
			List<UnitCount> defenderLosses,
			Guid reportId
		) {
			bool attackerWon = outcome == "Attacker won";
			bool draw = outcome == "Draw";
			string outcomeClass = attackerWon ? "outcome-victory" : draw ? "outcome-draw" : "outcome-defeat";
			string outcomeLabel = attackerWon ? "Victory!" : draw ? "Draw" : "Defeat";

			var sb = new StringBuilder();
			sb.AppendLine("<div class=\"battle-report\">");
			sb.AppendLine($"  <div class=\"outcome {outcomeClass}\">{outcomeLabel}</div>");
			sb.AppendLine($"  <p><strong>Attacker:</strong> {HtmlEncode(attackerName)} ({HtmlEncode(attackerRace)}) vs <strong>Defender:</strong> {HtmlEncode(defenderName)} ({HtmlEncode(defenderRace)})</p>");
			sb.AppendLine("  <table>");
			sb.AppendLine("    <thead><tr><th>Side</th><th>Unit</th><th>Lost</th></tr></thead>");
			sb.AppendLine("    <tbody>");
			foreach (var uc in attackerLosses) {
				var name = gameDef.GetUnitDef(uc.UnitDefId)?.Name ?? uc.UnitDefId.Id;
				sb.AppendLine($"      <tr><td>Attacker</td><td>{HtmlEncode(name)}</td><td>{uc.Count}</td></tr>");
			}
			foreach (var uc in defenderLosses) {
				var name = gameDef.GetUnitDef(uc.UnitDefId)?.Name ?? uc.UnitDefId.Id;
				sb.AppendLine($"      <tr><td>Defender</td><td>{HtmlEncode(name)}</td><td>{uc.Count}</td></tr>");
			}
			if (!attackerLosses.Any() && !defenderLosses.Any()) {
				sb.AppendLine("      <tr><td colspan=\"3\">No units lost</td></tr>");
			}
			sb.AppendLine("    </tbody>");
			sb.AppendLine("  </table>");
			if (landTransferred > 0 || workersCaptured > 0 || resourcesStolen.Count > 0) {
				sb.AppendLine("  <div class=\"spoils\">");
				if (landTransferred > 0) sb.AppendLine($"    <span class=\"badge\">+{landTransferred} land captured</span>");
				if (workersCaptured > 0) sb.AppendLine($"    <span class=\"badge\">+{workersCaptured} workers captured</span>");
				foreach (var kv in resourcesStolen) {
					sb.AppendLine($"    <span class=\"badge\">+{kv.Value} {HtmlEncode(kv.Key)} pillaged</span>");
				}
				sb.AppendLine("  </div>");
			}
			sb.AppendLine($"  <p><a href=\"/battles/{reportId}\">View detailed battle replay</a></p>");
			sb.AppendLine("</div>");
			return sb.ToString();
		}

		private static string HtmlEncode(string text) {
			return text
				.Replace("&", "&amp;")
				.Replace("<", "&lt;")
				.Replace(">", "&gt;")
				.Replace("\"", "&quot;");
		}
	}
}
