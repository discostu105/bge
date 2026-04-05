using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.Commands;
using System.Linq;
using Xunit;

namespace BrowserGameEngine.StatefulGameServer.Test {
	public class SpyMissionTest {
		private static readonly PlayerId Player1 = PlayerIdFactory.Create("player0");
		private static readonly PlayerId Player2 = PlayerIdFactory.Create("player1");

		[Fact]
		public void SendMission_ValidPlayer_ReturnsMissionIdAndResolveTime() {
			var game = new TestGame(playerCount: 2);

			var (missionId, estimatedResolveAt) = game.SpyMissionRepositoryWrite.SendMission(
				new SpyMissionCommand(Player1, Player2, SpyMissionType.Intelligence));

			Assert.NotEqual(System.Guid.Empty, missionId);
			Assert.True(estimatedResolveAt > System.DateTime.UtcNow);
		}

		[Fact]
		public void SendMission_DeductsCost() {
			var growthResourceId = Id.ResDef("res1");
			var game = new TestGame(playerCount: 2);
			var before = game.ResourceRepository.GetAmount(Player1, growthResourceId);

			game.SpyMissionRepositoryWrite.SendMission(
				new SpyMissionCommand(Player1, Player2, SpyMissionType.Intelligence));

			var after = game.ResourceRepository.GetAmount(Player1, growthResourceId);
			Assert.True(after < before, "Cost should have been deducted.");
		}

		[Fact]
		public void SendMission_InsufficientResources_ThrowsCannotAfford() {
			var game = new TestGame(playerCount: 2);
			var amount = game.ResourceRepository.GetAmount(Player1, Id.ResDef("res1"));
			game.ResourceRepositoryWrite.DeductCost(Player1, Id.ResDef("res1"), amount);

			Assert.Throws<CannotAffordException>(() =>
				game.SpyMissionRepositoryWrite.SendMission(
					new SpyMissionCommand(Player1, Player2, SpyMissionType.Intelligence)));
		}

		[Fact]
		public void SendMission_CreatesInTransitMission() {
			var game = new TestGame(playerCount: 2);

			game.SpyMissionRepositoryWrite.SendMission(
				new SpyMissionCommand(Player1, Player2, SpyMissionType.Sabotage));

			var missions = game.SpyMissionRepository.GetMissions(Player1);
			Assert.Single(missions);
			Assert.Equal(SpyMissionStatus.InTransit, missions[0].Status);
			Assert.Equal(SpyMissionType.Sabotage, missions[0].MissionType);
			Assert.Equal(Player2, missions[0].TargetPlayerId);
		}

		[Fact]
		public void ProcessMissions_BeforeTimerExpires_RemainsInTransit() {
			var game = new TestGame(playerCount: 2);
			game.SpyMissionRepositoryWrite.SendMission(
				new SpyMissionCommand(Player1, Player2, SpyMissionType.Intelligence));

			// Process once — timer should still be > 0
			game.SpyMissionRepositoryWrite.ProcessMissions(Player1);

			var missions = game.SpyMissionRepository.GetMissions(Player1);
			Assert.Equal(SpyMissionStatus.InTransit, missions[0].Status);
		}

		[Fact]
		public void ProcessMissions_AfterTimerExpires_MissionCompletes() {
			var game = new TestGame(playerCount: 2);
			game.SpyMissionRepositoryWrite.SendMission(
				new SpyMissionCommand(Player1, Player2, SpyMissionType.Intelligence));

			// Intelligence timer = 3 ticks
			for (int i = 0; i < 3; i++) {
				game.SpyMissionRepositoryWrite.ProcessMissions(Player1);
			}

			var missions = game.SpyMissionRepository.GetMissions(Player1);
			// Mission is either Completed or Intercepted
			Assert.NotEqual(SpyMissionStatus.InTransit, missions[0].Status);
		}

		[Fact]
		public void ProcessMissions_Sabotage_DeductsTargetResources() {
			// Ensure target has resources to deduct and attacker has no counter-intel (0 detection chance)
			var game = new TestGame(playerCount: 2);
			var growthResourceId = Id.ResDef("res1");
			var targetBefore = game.ResourceRepository.GetAmount(Player2, growthResourceId);

			game.SpyMissionRepositoryWrite.SendMission(
				new SpyMissionCommand(Player1, Player2, SpyMissionType.Sabotage));

			// Sabotage timer = 5 ticks
			for (int i = 0; i < 5; i++) {
				game.SpyMissionRepositoryWrite.ProcessMissions(Player1);
			}

			var missions = game.SpyMissionRepository.GetMissions(Player1);
			// No counter-intel tech in TestGame so detection probability = 0; mission always completes
			Assert.Equal(SpyMissionStatus.Completed, missions[0].Status);
			var targetAfter = game.ResourceRepository.GetAmount(Player2, growthResourceId);
			Assert.True(targetAfter < targetBefore, "Sabotage should have reduced target resources.");
		}

		[Fact]
		public void ProcessMissions_StealResources_TransfersToAttacker() {
			var game = new TestGame(playerCount: 2);
			var growthResourceId = Id.ResDef("res1");

			game.SpyMissionRepositoryWrite.SendMission(
				new SpyMissionCommand(Player1, Player2, SpyMissionType.StealResources));

			// Record balances after sending (cost deducted)
			var attackerBefore = game.ResourceRepository.GetAmount(Player1, growthResourceId);
			var targetBefore = game.ResourceRepository.GetAmount(Player2, growthResourceId);

			// StealResources timer = 4 ticks
			for (int i = 0; i < 4; i++) {
				game.SpyMissionRepositoryWrite.ProcessMissions(Player1);
			}

			var missions = game.SpyMissionRepository.GetMissions(Player1);
			// No counter-intel tech in TestGame so detection probability = 0; mission always completes
			Assert.Equal(SpyMissionStatus.Completed, missions[0].Status);
			var attackerAfter = game.ResourceRepository.GetAmount(Player1, growthResourceId);
			var targetAfter = game.ResourceRepository.GetAmount(Player2, growthResourceId);
			Assert.True(attackerAfter > attackerBefore, "Attacker should have gained resources from steal.");
			Assert.True(targetAfter < targetBefore, "Target should have lost resources from steal.");
		}

		[Fact]
		public void GetActiveMissions_ReturnsOnlyInTransit() {
			var game = new TestGame(playerCount: 2);
			game.SpyMissionRepositoryWrite.SendMission(
				new SpyMissionCommand(Player1, Player2, SpyMissionType.Intelligence));

			var active = game.SpyMissionRepository.GetActiveMissions(Player1);
			Assert.Single(active);
			Assert.Equal(SpyMissionStatus.InTransit, active[0].Status);
		}

		[Fact]
		public void SendMission_MultipleMissions_AllTracked() {
			var game = new TestGame(playerCount: 2);

			game.SpyMissionRepositoryWrite.SendMission(
				new SpyMissionCommand(Player1, Player2, SpyMissionType.Intelligence));
			game.SpyMissionRepositoryWrite.SendMission(
				new SpyMissionCommand(Player1, Player2, SpyMissionType.Sabotage));

			var missions = game.SpyMissionRepository.GetMissions(Player1);
			Assert.Equal(2, missions.Count);
		}
	}
}
