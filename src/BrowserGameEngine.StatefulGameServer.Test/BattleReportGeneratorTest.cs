using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using BrowserGameEngine.StatefulGameServer.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace BrowserGameEngine.StatefulGameServer.Test {
	public class BattleReportGeneratorTest {

		private static readonly PlayerId Player1 = PlayerIdFactory.Create("player0");
		private static readonly PlayerId Player2 = PlayerIdFactory.Create("player1");

		private BattleResult CreateBattleResult(
			List<UnitCount> attackersSurvived,
			List<UnitCount> defendersSurvived,
			List<UnitCount> attackersDestroyed,
			List<UnitCount> defendersDestroyed,
			List<BattleRoundSnapshotImmutable>? rounds = null,
			List<Cost>? resourcesStolen = null,
			decimal landTransferred = 0,
			int workersCaptured = 0
		) {
			return new BattleResult {
				Attacker = Player1,
				Defender = Player2,
				BtlResult = new BtlResult {
					AttackingUnitsSurvived = attackersSurvived,
					DefendingUnitsSurvived = defendersSurvived,
					AttackingUnitsDestroyed = attackersDestroyed,
					DefendingUnitsDestroyed = defendersDestroyed,
					ResourcesDestroyed = new List<Cost>(),
					ResourcesStolen = resourcesStolen ?? new List<Cost>(),
					LandTransferred = landTransferred,
					WorkersCaptured = workersCaptured,
					TotalAttackerStrengthBefore = 500,
					TotalDefenderStrengthBefore = 300,
					Rounds = rounds ?? new List<BattleRoundSnapshotImmutable>()
				}
			};
		}

		private (TestGame game, BattleReportGenerator generator) CreateGenerator() {
			var game = new TestGame(playerCount: 2);
			var generator = new BattleReportGenerator(
				game.PlayerRepository,
				game.MessageRepositoryWrite,
				game.BattleReportRepositoryWrite,
				game.GameDef,
				new NullPlayerNotificationService(),
				NullNotificationService.Instance,
				TimeProvider.System
			);
			return (game, generator);
		}

		[Fact]
		public void GenerateReports_AttackerWins_SetsCorrectOutcome() {
			var (game, generator) = CreateGenerator();

			var result = CreateBattleResult(
				attackersSurvived: new List<UnitCount> { new UnitCount(Id.UnitDef("unit1"), 5) },
				defendersSurvived: new List<UnitCount>(),
				attackersDestroyed: new List<UnitCount> { new UnitCount(Id.UnitDef("unit1"), 5) },
				defendersDestroyed: new List<UnitCount> { new UnitCount(Id.UnitDef("unit1"), 10) }
			);

			generator.GenerateReports(result);

			var reports = game.BattleReportRepository.GetBattleReports(Player1);
			Assert.Single(reports);
			Assert.Equal("Attacker won", reports[0].Outcome);
			Assert.Equal(500, reports[0].TotalAttackerStrengthBefore);
			Assert.Equal(300, reports[0].TotalDefenderStrengthBefore);
		}

		[Fact]
		public void GenerateReports_DefenderWins_SetsCorrectOutcome() {
			var (game, generator) = CreateGenerator();

			var result = CreateBattleResult(
				attackersSurvived: new List<UnitCount>(),
				defendersSurvived: new List<UnitCount> { new UnitCount(Id.UnitDef("unit1"), 5) },
				attackersDestroyed: new List<UnitCount> { new UnitCount(Id.UnitDef("unit1"), 10) },
				defendersDestroyed: new List<UnitCount> { new UnitCount(Id.UnitDef("unit1"), 5) }
			);

			generator.GenerateReports(result);

			var reports = game.BattleReportRepository.GetBattleReports(Player1);
			Assert.Single(reports);
			Assert.Equal("Defender won", reports[0].Outcome);
		}

		[Fact]
		public void GenerateReports_Draw_SetsCorrectOutcome() {
			var (game, generator) = CreateGenerator();

			var result = CreateBattleResult(
				attackersSurvived: new List<UnitCount>(),
				defendersSurvived: new List<UnitCount>(),
				attackersDestroyed: new List<UnitCount> { new UnitCount(Id.UnitDef("unit1"), 10) },
				defendersDestroyed: new List<UnitCount> { new UnitCount(Id.UnitDef("unit1"), 10) }
			);

			generator.GenerateReports(result);

			var reports = game.BattleReportRepository.GetBattleReports(Player1);
			Assert.Single(reports);
			Assert.Equal("Draw", reports[0].Outcome);
		}

		[Fact]
		public void GenerateReports_WithRounds_ComputesInitialUnitsFromRoundData() {
			var (game, generator) = CreateGenerator();

			var rounds = new List<BattleRoundSnapshotImmutable> {
				new BattleRoundSnapshotImmutable(
					RoundNumber: 1,
					AttackerUnitsRemaining: new List<UnitCount> { new UnitCount(Id.UnitDef("unit1"), 8) },
					DefenderUnitsRemaining: new List<UnitCount> { new UnitCount(Id.UnitDef("unit1"), 6) },
					AttackerCasualties: new List<UnitCount> { new UnitCount(Id.UnitDef("unit1"), 2) },
					DefenderCasualties: new List<UnitCount> { new UnitCount(Id.UnitDef("unit1"), 4) }
				),
				new BattleRoundSnapshotImmutable(
					RoundNumber: 2,
					AttackerUnitsRemaining: new List<UnitCount> { new UnitCount(Id.UnitDef("unit1"), 5) },
					DefenderUnitsRemaining: new List<UnitCount>(),
					AttackerCasualties: new List<UnitCount> { new UnitCount(Id.UnitDef("unit1"), 3) },
					DefenderCasualties: new List<UnitCount> { new UnitCount(Id.UnitDef("unit1"), 6) }
				)
			};

			var result = CreateBattleResult(
				attackersSurvived: new List<UnitCount> { new UnitCount(Id.UnitDef("unit1"), 5) },
				defendersSurvived: new List<UnitCount>(),
				attackersDestroyed: new List<UnitCount> { new UnitCount(Id.UnitDef("unit1"), 5) },
				defendersDestroyed: new List<UnitCount> { new UnitCount(Id.UnitDef("unit1"), 10) },
				rounds: rounds
			);

			generator.GenerateReports(result);

			var reports = game.BattleReportRepository.GetBattleReports(Player1);
			Assert.Single(reports);
			// Initial units = round[0].Remaining + round[0].Casualties
			Assert.Equal(10, reports[0].AttackerUnitsInitial.Sum(u => u.Count)); // 8 + 2
			Assert.Equal(10, reports[0].DefenderUnitsInitial.Sum(u => u.Count)); // 6 + 4
			Assert.Equal(2, reports[0].Rounds.Count);
		}

		[Fact]
		public void GenerateReports_WithoutRounds_ComputesInitialUnitsFromSurvivedPlusDestroyed() {
			var (game, generator) = CreateGenerator();

			var result = CreateBattleResult(
				attackersSurvived: new List<UnitCount> { new UnitCount(Id.UnitDef("unit1"), 7) },
				defendersSurvived: new List<UnitCount>(),
				attackersDestroyed: new List<UnitCount> { new UnitCount(Id.UnitDef("unit1"), 3) },
				defendersDestroyed: new List<UnitCount> { new UnitCount(Id.UnitDef("unit1"), 8) }
			);

			generator.GenerateReports(result);

			var reports = game.BattleReportRepository.GetBattleReports(Player1);
			Assert.Single(reports);
			// No rounds → initial = survived + destroyed
			Assert.Equal(10, reports[0].AttackerUnitsInitial.Sum(u => u.Count)); // 7 + 3
			Assert.Equal(8, reports[0].DefenderUnitsInitial.Sum(u => u.Count)); // 0 + 8
		}

		[Fact]
		public void GenerateReports_WithResourcesStolen_AggregatesCorrectly() {
			var (game, generator) = CreateGenerator();

			var stolen = new List<Cost> {
				CostHelper.Create(("res1", 50m)),
				CostHelper.Create(("res1", 30m), ("res2", 20m))
			};

			var result = CreateBattleResult(
				attackersSurvived: new List<UnitCount> { new UnitCount(Id.UnitDef("unit1"), 5) },
				defendersSurvived: new List<UnitCount>(),
				attackersDestroyed: new List<UnitCount>(),
				defendersDestroyed: new List<UnitCount> { new UnitCount(Id.UnitDef("unit1"), 5) },
				resourcesStolen: stolen,
				landTransferred: 10,
				workersCaptured: 3
			);

			generator.GenerateReports(result);

			var reports = game.BattleReportRepository.GetBattleReports(Player1);
			Assert.Single(reports);
			Assert.Equal(80m, reports[0].ResourcesStolen["res1"]); // 50 + 30
			Assert.Equal(20m, reports[0].ResourcesStolen["res2"]);
			Assert.Equal(10, reports[0].LandTransferred);
			Assert.Equal(3, reports[0].WorkersCaptured);
		}

		[Fact]
		public void GenerateReports_SendsMessagesToBothPlayers() {
			var (game, generator) = CreateGenerator();

			var result = CreateBattleResult(
				attackersSurvived: new List<UnitCount> { new UnitCount(Id.UnitDef("unit1"), 5) },
				defendersSurvived: new List<UnitCount>(),
				attackersDestroyed: new List<UnitCount>(),
				defendersDestroyed: new List<UnitCount> { new UnitCount(Id.UnitDef("unit1"), 5) }
			);

			generator.GenerateReports(result);

			var attackerMessages = game.MessageRepository.GetMessages(Player1);
			var defenderMessages = game.MessageRepository.GetMessages(Player2);
			Assert.Single(attackerMessages);
			Assert.Single(defenderMessages);
			Assert.Contains("Battle Report", attackerMessages[0].Subject);
			Assert.Contains("Battle Report", defenderMessages[0].Subject);
		}

		[Fact]
		public void GenerateReports_BothPlayersGetBattleReport() {
			var (game, generator) = CreateGenerator();

			var result = CreateBattleResult(
				attackersSurvived: new List<UnitCount> { new UnitCount(Id.UnitDef("unit1"), 5) },
				defendersSurvived: new List<UnitCount>(),
				attackersDestroyed: new List<UnitCount>(),
				defendersDestroyed: new List<UnitCount> { new UnitCount(Id.UnitDef("unit1"), 5) }
			);

			generator.GenerateReports(result);

			var p1Reports = game.BattleReportRepository.GetBattleReports(Player1);
			var p2Reports = game.BattleReportRepository.GetBattleReports(Player2);
			Assert.Single(p1Reports);
			Assert.Single(p2Reports);
			Assert.Equal(p1Reports[0].Id, p2Reports[0].Id);
		}

		[Fact]
		public void GenerateReports_MessageBodyContainsBattleReplayLink() {
			var (game, generator) = CreateGenerator();

			var result = CreateBattleResult(
				attackersSurvived: new List<UnitCount> { new UnitCount(Id.UnitDef("unit1"), 5) },
				defendersSurvived: new List<UnitCount>(),
				attackersDestroyed: new List<UnitCount>(),
				defendersDestroyed: new List<UnitCount> { new UnitCount(Id.UnitDef("unit1"), 5) }
			);

			generator.GenerateReports(result);

			var messages = game.MessageRepository.GetMessages(Player1);
			Assert.Contains("/battles/", messages[0].Body);
			Assert.Contains("battle replay", messages[0].Body);
		}

		[Fact]
		public void GenerateReports_MessageBodyContainsOutcomeAndNames() {
			var (game, generator) = CreateGenerator();

			var result = CreateBattleResult(
				attackersSurvived: new List<UnitCount>(),
				defendersSurvived: new List<UnitCount>(),
				attackersDestroyed: new List<UnitCount> { new UnitCount(Id.UnitDef("unit1"), 10) },
				defendersDestroyed: new List<UnitCount> { new UnitCount(Id.UnitDef("unit1"), 10) }
			);

			generator.GenerateReports(result);

			var messages = game.MessageRepository.GetMessages(Player1);
			Assert.Contains("Draw", messages[0].Body);
			Assert.Contains("Attacker", messages[0].Body);
			Assert.Contains("Defender", messages[0].Body);
		}

		[Fact]
		public void GenerateReports_SetsRaceFromPlayerType() {
			var (game, generator) = CreateGenerator();

			var result = CreateBattleResult(
				attackersSurvived: new List<UnitCount> { new UnitCount(Id.UnitDef("unit1"), 5) },
				defendersSurvived: new List<UnitCount>(),
				attackersDestroyed: new List<UnitCount>(),
				defendersDestroyed: new List<UnitCount> { new UnitCount(Id.UnitDef("unit1"), 5) }
			);

			generator.GenerateReports(result);

			var reports = game.BattleReportRepository.GetBattleReports(Player1);
			// TestGameDefFactory defines PlayerTypes "type1" and "type2"
			Assert.NotNull(reports[0].AttackerRace);
			Assert.NotNull(reports[0].DefenderRace);
			Assert.NotEmpty(reports[0].AttackerRace);
		}

		[Fact]
		public void GenerateReports_MessageBodyContainsSpoilsWhenPresent() {
			var (game, generator) = CreateGenerator();

			var stolen = new List<Cost> {
				CostHelper.Create(("res1", 100m))
			};

			var result = CreateBattleResult(
				attackersSurvived: new List<UnitCount> { new UnitCount(Id.UnitDef("unit1"), 5) },
				defendersSurvived: new List<UnitCount>(),
				attackersDestroyed: new List<UnitCount>(),
				defendersDestroyed: new List<UnitCount> { new UnitCount(Id.UnitDef("unit1"), 5) },
				resourcesStolen: stolen,
				landTransferred: 5,
				workersCaptured: 2
			);

			generator.GenerateReports(result);

			var messages = game.MessageRepository.GetMessages(Player1);
			Assert.Contains("land captured", messages[0].Body);
			Assert.Contains("workers captured", messages[0].Body);
			Assert.Contains("pillaged", messages[0].Body);
		}

		/// <summary>No-op implementation of IPlayerNotificationService for tests.</summary>
		private class NullPlayerNotificationService : IPlayerNotificationService {
			public void Push(string userId, string message, NotificationKind kind) { }
			public List<PlayerNotification> GetRecent(string userId, int limit = 20) => new();
			public void ClearAll(string userId) { }
		}
	}
}
