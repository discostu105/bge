using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using System;
using System.Linq;
using Xunit;

namespace BrowserGameEngine.StatefulGameServer.Test {
	public class PlayerRepositoryTest {

		[Fact]
		public void DeletePlayer_RemovesPlayerFromWorld() {
			var game = new TestGame(playerCount: 1);
			var playerId = PlayerIdFactory.Create("player0");
			var userRepository = new UserRepository(game.GlobalState, game.World);

			game.PlayerRepositoryWrite.DeletePlayer(playerId);

			Assert.False(game.PlayerRepository.Exists(playerId));
		}

		[Fact]
		public void DeletePlayer_NoLongerAppearsInGetPlayersForUser() {
			var game = new TestGame(playerCount: 1);
			var playerId = PlayerIdFactory.Create("player0");
			var userId = "testuser";
			// Assign a userId to the player via creating a fresh player
			game.PlayerRepositoryWrite.CreatePlayer(PlayerIdFactory.Create("newplayer"), userId);
			var userRepository = new UserRepository(game.GlobalState, game.World);

			game.PlayerRepositoryWrite.DeletePlayer(PlayerIdFactory.Create("newplayer"));

			var players = userRepository.GetPlayersForUser(userId).ToList();
			Assert.DoesNotContain(players, p => p.PlayerId == PlayerIdFactory.Create("newplayer"));
		}

		[Fact]
		public void DeletePlayer_GetThrowsAfterDeletion() {
			var game = new TestGame(playerCount: 1);
			var playerId = PlayerIdFactory.Create("player0");

			game.PlayerRepositoryWrite.DeletePlayer(playerId);

			Assert.ThrowsAny<Exception>(() => game.PlayerRepository.Get(playerId));
		}

		[Fact]
		public void IsPlayerAttackable_SamePlayer_ReturnsFalse() {
			var game = new TestGame(playerCount: 2);
			var player1 = PlayerIdFactory.Create("player0");

			Assert.False(game.PlayerRepository.IsPlayerAttackable(player1, player1));
		}

		[Fact]
		public void IsPlayerAttackable_EqualScore_ReturnsTrue() {
			var game = new TestGame(playerCount: 2);
			var player1 = PlayerIdFactory.Create("player0");
			var player2 = PlayerIdFactory.Create("player1");

			// Both players start with the same resources (score=1000), so each is attackable by the other
			Assert.True(game.PlayerRepository.IsPlayerAttackable(player1, player2));
			Assert.True(game.PlayerRepository.IsPlayerAttackable(player2, player1));
		}

		[Fact]
		public void IsPlayerAttackable_DefenderBelowMinScore_ReturnsFalse() {
			var game = new TestGame(playerCount: 2);
			var player1 = PlayerIdFactory.Create("player0");
			var player2 = PlayerIdFactory.Create("player1");

			// Players start with land=50; bump player1 well above the 50% threshold.
			game.ResourceRepositoryWrite.AddResources(player1, Id.ResDef("land"), 9000); // player1 land = 9050
			// player2 land = 50, which is < 9050 * 0.5 = 4525

			Assert.False(game.PlayerRepository.IsPlayerAttackable(player1, player2));
		}

		[Fact]
		public void IsPlayerAttackable_DefenderAboveMinLand_ReturnsTrue() {
			var game = new TestGame(playerCount: 2);
			var player1 = PlayerIdFactory.Create("player0");
			var player2 = PlayerIdFactory.Create("player1");

			game.ResourceRepositoryWrite.AddResources(player1, Id.ResDef("land"), 500); // player1 land = 550
			game.ResourceRepositoryWrite.AddResources(player2, Id.ResDef("land"), 400); // player2 land = 450 ≥ 275

			Assert.True(game.PlayerRepository.IsPlayerAttackable(player1, player2));
		}

		[Fact]
		public void GetAttackablePlayers_ExcludesSelf() {
			var game = new TestGame(playerCount: 3);
			var player1 = PlayerIdFactory.Create("player0");

			var attackable = game.PlayerRepository.GetAttackablePlayers(player1).ToList();

			Assert.DoesNotContain(attackable, p => p.PlayerId == player1);
		}

		[Fact]
		public void GetAttackablePlayers_ExcludesPlayersBelowMinScore() {
			var game = new TestGame(playerCount: 2);
			var player1 = PlayerIdFactory.Create("player0");
			var player2 = PlayerIdFactory.Create("player1");

			// Players start with land=50; bump player1 so player2 falls below 50% threshold.
			game.ResourceRepositoryWrite.AddResources(player1, Id.ResDef("land"), 9000);

			var attackable = game.PlayerRepository.GetAttackablePlayers(player1).ToList();

			Assert.DoesNotContain(attackable, p => p.PlayerId == player2);
		}

		[Fact]
		public void GetIneligibilityReason_SamePlayer_ReturnsSelfAttack() {
			var game = new TestGame(playerCount: 2);
			var player1 = PlayerIdFactory.Create("player0");

			Assert.Equal(AttackIneligibilityReason.SelfAttack, game.PlayerRepository.GetIneligibilityReason(player1, player1));
		}

		[Fact]
		public void GetIneligibilityReason_DefenderBelowMinScore_ReturnsLandTooSmall() {
			var game = new TestGame(playerCount: 2);
			var player1 = PlayerIdFactory.Create("player0");
			var player2 = PlayerIdFactory.Create("player1");

			// Players start with land=50; bump player1 well above the 50% threshold.
			game.ResourceRepositoryWrite.AddResources(player1, Id.ResDef("land"), 9000); // player1 land = 9050
			// player2 land = 50, which is < 9050 * 0.5 = 4525

			Assert.Equal(AttackIneligibilityReason.LandTooSmall, game.PlayerRepository.GetIneligibilityReason(player1, player2));
		}

		[Fact]
		public void GetIneligibilityReason_EqualScore_ReturnsNull() {
			var game = new TestGame(playerCount: 2);
			var player1 = PlayerIdFactory.Create("player0");
			var player2 = PlayerIdFactory.Create("player1");

			// Both players start with equal scores
			Assert.Null(game.PlayerRepository.GetIneligibilityReason(player1, player2));
		}

		[Fact]
		public void GetIneligibilityReason_DefenderAboveMinLand_ReturnsNull() {
			var game = new TestGame(playerCount: 2);
			var player1 = PlayerIdFactory.Create("player0");
			var player2 = PlayerIdFactory.Create("player1");

			game.ResourceRepositoryWrite.AddResources(player1, Id.ResDef("land"), 500); // player1 land = 550
			game.ResourceRepositoryWrite.AddResources(player2, Id.ResDef("land"), 400); // player2 land = 450 ≥ 275

			Assert.Null(game.PlayerRepository.GetIneligibilityReason(player1, player2));
		}

		[Fact]
		public void BanPlayer_SetsIsBannedTrue() {
			var game = new TestGame(playerCount: 1);
			var playerId = PlayerIdFactory.Create("player0");

			game.PlayerRepositoryWrite.BanPlayer(playerId);

			var player = game.PlayerRepository.Get(playerId);
			Assert.True(player.IsBanned);
		}

		[Fact]
		public void UnbanPlayer_SetsIsBannedFalse() {
			var game = new TestGame(playerCount: 1);
			var playerId = PlayerIdFactory.Create("player0");

			game.PlayerRepositoryWrite.BanPlayer(playerId);
			game.PlayerRepositoryWrite.UnbanPlayer(playerId);

			var player = game.PlayerRepository.Get(playerId);
			Assert.False(player.IsBanned);
		}

		[Fact]
		public void BanPlayer_IsBannedSurvivesSerialization() {
			var game = new TestGame(playerCount: 1);
			var playerId = PlayerIdFactory.Create("player0");

			game.PlayerRepositoryWrite.BanPlayer(playerId);

			// Round-trip through immutable/mutable
			var immutable = game.World.ToImmutable();
			var game2 = new TestGame(immutable);
			var player = game2.PlayerRepository.Get(playerId);
			Assert.True(player.IsBanned);
		}
	}
}
