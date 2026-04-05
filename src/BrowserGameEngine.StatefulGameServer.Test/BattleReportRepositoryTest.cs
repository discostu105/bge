using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace BrowserGameEngine.StatefulGameServer.Test {
	public class BattleReportRepositoryTest {

		private static readonly PlayerId Player1 = PlayerIdFactory.Create("player0");
		private static readonly PlayerId Player2 = PlayerIdFactory.Create("player1");

		private BattleReport CreateTestReport(Guid? id = null) {
			return new BattleReport {
				Id = id ?? Guid.NewGuid(),
				AttackerId = Player1,
				DefenderId = Player2,
				AttackerName = "Attacker",
				DefenderName = "Defender",
				AttackerRace = "Terran",
				DefenderRace = "Zerg",
				Outcome = "Attacker won",
				TotalAttackerStrengthBefore = 100,
				TotalDefenderStrengthBefore = 80,
				AttackerUnitsInitial = new List<UnitCount> {
					new UnitCount(Id.UnitDef("marine"), 10)
				},
				DefenderUnitsInitial = new List<UnitCount> {
					new UnitCount(Id.UnitDef("zergling"), 8)
				},
				Rounds = new List<BattleRoundSnapshotImmutable> {
					new BattleRoundSnapshotImmutable(
						RoundNumber: 1,
						AttackerUnitsRemaining: new List<UnitCount> { new UnitCount(Id.UnitDef("marine"), 9) },
						DefenderUnitsRemaining: new List<UnitCount> { new UnitCount(Id.UnitDef("zergling"), 5) },
						AttackerCasualties: new List<UnitCount> { new UnitCount(Id.UnitDef("marine"), 1) },
						DefenderCasualties: new List<UnitCount> { new UnitCount(Id.UnitDef("zergling"), 3) }
					)
				},
				LandTransferred = 5,
				WorkersCaptured = 2,
				ResourcesStolen = new Dictionary<string, decimal> { { "minerals", 100m } },
				CreatedAt = DateTime.UtcNow
			};
		}

		[Fact]
		public void AddBattleReport_CanBeRetrievedById() {
			var game = new TestGame(playerCount: 2);
			var reportId = Guid.NewGuid();
			var report = CreateTestReport(reportId);

			game.BattleReportRepositoryWrite.AddBattleReport(Player1, report);

			var retrieved = game.BattleReportRepository.GetBattleReport(Player1, reportId);
			Assert.NotNull(retrieved);
			Assert.Equal(reportId, retrieved!.Id);
			Assert.Equal("Attacker", retrieved.AttackerName);
			Assert.Equal("Defender", retrieved.DefenderName);
			Assert.Equal("Attacker won", retrieved.Outcome);
		}

		[Fact]
		public void GetBattleReport_NonExistentId_ReturnsNull() {
			var game = new TestGame(playerCount: 2);
			game.BattleReportRepositoryWrite.AddBattleReport(Player1, CreateTestReport());

			var result = game.BattleReportRepository.GetBattleReport(Player1, Guid.NewGuid());
			Assert.Null(result);
		}

		[Fact]
		public void GetBattleReports_ReturnsAllReports() {
			var game = new TestGame(playerCount: 2);

			game.BattleReportRepositoryWrite.AddBattleReport(Player1, CreateTestReport());
			game.BattleReportRepositoryWrite.AddBattleReport(Player1, CreateTestReport());
			game.BattleReportRepositoryWrite.AddBattleReport(Player1, CreateTestReport());

			var reports = game.BattleReportRepository.GetBattleReports(Player1);
			Assert.Equal(3, reports.Count);
		}

		[Fact]
		public void AddBattleReport_ExceedsMaxReports_OldestRemoved() {
			var game = new TestGame(playerCount: 2);
			var oldestId = Guid.NewGuid();

			game.BattleReportRepositoryWrite.AddBattleReport(Player1, CreateTestReport(oldestId));
			for (int i = 1; i < BattleReportRepositoryWrite.MaxReportsPerPlayer; i++) {
				game.BattleReportRepositoryWrite.AddBattleReport(Player1, CreateTestReport());
			}

			Assert.Equal(BattleReportRepositoryWrite.MaxReportsPerPlayer,
				game.BattleReportRepository.GetBattleReports(Player1).Count);

			// Adding one more should evict the oldest
			var newestId = Guid.NewGuid();
			game.BattleReportRepositoryWrite.AddBattleReport(Player1, CreateTestReport(newestId));

			var reports = game.BattleReportRepository.GetBattleReports(Player1);
			Assert.Equal(BattleReportRepositoryWrite.MaxReportsPerPlayer, reports.Count);
			Assert.Null(game.BattleReportRepository.GetBattleReport(Player1, oldestId));
			Assert.NotNull(game.BattleReportRepository.GetBattleReport(Player1, newestId));
		}

		[Fact]
		public void AddBattleReport_DifferentPlayers_ReportsIsolated() {
			var game = new TestGame(playerCount: 2);
			var reportId = Guid.NewGuid();
			var report = CreateTestReport(reportId);

			game.BattleReportRepositoryWrite.AddBattleReport(Player1, report);

			Assert.NotNull(game.BattleReportRepository.GetBattleReport(Player1, reportId));
			Assert.Empty(game.BattleReportRepository.GetBattleReports(Player2));
		}

		[Fact]
		public void GetBattleReports_EmptyByDefault() {
			var game = new TestGame(playerCount: 2);
			var reports = game.BattleReportRepository.GetBattleReports(Player1);
			Assert.Empty(reports);
		}

		[Fact]
		public void AddBattleReport_BothPlayersGetSameReport() {
			var game = new TestGame(playerCount: 2);
			var reportId = Guid.NewGuid();
			var report = CreateTestReport(reportId);

			game.BattleReportRepositoryWrite.AddBattleReport(Player1, report);
			game.BattleReportRepositoryWrite.AddBattleReport(Player2, report);

			var p1Report = game.BattleReportRepository.GetBattleReport(Player1, reportId);
			var p2Report = game.BattleReportRepository.GetBattleReport(Player2, reportId);
			Assert.NotNull(p1Report);
			Assert.NotNull(p2Report);
			Assert.Equal(p1Report!.Id, p2Report!.Id);
			Assert.Equal("Attacker won", p1Report.Outcome);
			Assert.Equal(100, p1Report.TotalAttackerStrengthBefore);
			Assert.Equal(80, p1Report.TotalDefenderStrengthBefore);
		}

		[Fact]
		public void BattleReport_PreservesRoundDetails() {
			var game = new TestGame(playerCount: 2);
			var report = CreateTestReport();
			game.BattleReportRepositoryWrite.AddBattleReport(Player1, report);

			var retrieved = game.BattleReportRepository.GetBattleReports(Player1)[0];
			Assert.Single(retrieved.Rounds);
			var round = retrieved.Rounds[0];
			Assert.Equal(1, round.RoundNumber);
			Assert.Single(round.AttackerUnitsRemaining);
			Assert.Equal(9, round.AttackerUnitsRemaining[0].Count);
			Assert.Single(round.DefenderUnitsRemaining);
			Assert.Equal(5, round.DefenderUnitsRemaining[0].Count);
			Assert.Single(round.AttackerCasualties);
			Assert.Equal(1, round.AttackerCasualties[0].Count);
			Assert.Single(round.DefenderCasualties);
			Assert.Equal(3, round.DefenderCasualties[0].Count);
		}

		[Fact]
		public void BattleReport_PreservesResourcesAndSpoils() {
			var game = new TestGame(playerCount: 2);
			var report = CreateTestReport();
			game.BattleReportRepositoryWrite.AddBattleReport(Player1, report);

			var retrieved = game.BattleReportRepository.GetBattleReports(Player1)[0];
			Assert.Equal(5, retrieved.LandTransferred);
			Assert.Equal(2, retrieved.WorkersCaptured);
			Assert.Single(retrieved.ResourcesStolen);
			Assert.Equal(100m, retrieved.ResourcesStolen["minerals"]);
			Assert.Equal("Terran", retrieved.AttackerRace);
			Assert.Equal("Zerg", retrieved.DefenderRace);
		}

		[Fact]
		public void BattleReport_ToImmutable_RoundTrip() {
			var report = CreateTestReport();
			var immutable = report.ToImmutable();
			var roundTripped = BattleReport.FromImmutable(immutable);

			Assert.Equal(report.Id, roundTripped.Id);
			Assert.Equal(report.AttackerId, roundTripped.AttackerId);
			Assert.Equal(report.DefenderId, roundTripped.DefenderId);
			Assert.Equal(report.AttackerName, roundTripped.AttackerName);
			Assert.Equal(report.DefenderName, roundTripped.DefenderName);
			Assert.Equal(report.Outcome, roundTripped.Outcome);
			Assert.Equal(report.TotalAttackerStrengthBefore, roundTripped.TotalAttackerStrengthBefore);
			Assert.Equal(report.TotalDefenderStrengthBefore, roundTripped.TotalDefenderStrengthBefore);
			Assert.Equal(report.LandTransferred, roundTripped.LandTransferred);
			Assert.Equal(report.WorkersCaptured, roundTripped.WorkersCaptured);
			Assert.Equal(report.Rounds.Count, roundTripped.Rounds.Count);
			Assert.Equal(report.AttackerUnitsInitial.Count, roundTripped.AttackerUnitsInitial.Count);
			Assert.Equal(report.DefenderUnitsInitial.Count, roundTripped.DefenderUnitsInitial.Count);
			Assert.Equal(report.ResourcesStolen["minerals"], roundTripped.ResourcesStolen["minerals"]);
		}
	}
}
