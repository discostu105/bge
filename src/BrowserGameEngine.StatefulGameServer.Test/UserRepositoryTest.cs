using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.Test;
using System;
using System.Linq;
using Xunit;

namespace BrowserGameEngine.StatefulGameServer.Test {
	public class UserRepositoryTest {
		[Fact]
		public void CreateUser_NewGithubId_StoresUser() {
			var game = new TestGame();
			var userRepo = new UserRepository(game.GlobalState, game.Accessor);
			var userRepoWrite = new UserRepositoryWrite(game.GlobalState, game.World, TimeProvider.System);

			var user = userRepoWrite.CreateUser("gh123", "octocat", "The Octocat");

			Assert.NotNull(user);
			Assert.Equal("gh123", user.GithubId);
			Assert.Equal("octocat", user.GithubLogin);
			Assert.Equal("The Octocat", user.DisplayName);
			Assert.False(string.IsNullOrEmpty(user.UserId));
		}

		[Fact]
		public void GetByGithubId_ExistingUser_ReturnsUser() {
			var game = new TestGame();
			var userRepo = new UserRepository(game.GlobalState, game.Accessor);
			var userRepoWrite = new UserRepositoryWrite(game.GlobalState, game.World, TimeProvider.System);

			userRepoWrite.CreateUser("gh456", "monalisa", "Mona Lisa");

			var found = userRepo.GetByGithubId("gh456");
			Assert.NotNull(found);
			Assert.Equal("monalisa", found!.GithubLogin);
		}

		[Fact]
		public void GetByGithubId_MissingUser_ReturnsNull() {
			var game = new TestGame();
			var userRepo = new UserRepository(game.GlobalState, game.Accessor);

			var result = userRepo.GetByGithubId("nonexistent");
			Assert.Null(result);
		}

		[Fact]
		public void CreateUser_DuplicateGithubId_Throws() {
			var game = new TestGame();
			var userRepoWrite = new UserRepositoryWrite(game.GlobalState, game.World, TimeProvider.System);
			userRepoWrite.CreateUser("dup123", "dup", "Dup");

			Assert.Throws<InvalidOperationException>(() =>
				userRepoWrite.CreateUser("dup123", "dup2", "Dup2"));
		}

		[Fact]
		public void GetPlayersForUser_AfterCreatePlayer_ReturnsPlayer() {
			var game = new TestGame();
			var userRepo = new UserRepository(game.GlobalState, game.Accessor);
			var userRepoWrite = new UserRepositoryWrite(game.GlobalState, game.World, TimeProvider.System);

			var user = userRepoWrite.CreateUser("gh789", "devuser", "Dev User");
			var playerId = PlayerIdFactory.Create(Guid.NewGuid().ToString());
			game.PlayerRepositoryWrite.CreatePlayer(playerId, user.UserId);

			var players = userRepo.GetPlayersForUser(user.UserId).ToList();
			Assert.Single(players);
			Assert.Equal(playerId, players[0].PlayerId);
			Assert.Equal(user.UserId, players[0].UserId);
		}

		[Fact]
		public void SetApiKeyHash_AndLookup_FindsPlayer() {
			var game = new TestGame();
			var userRepo = new UserRepository(game.GlobalState, game.Accessor);
			var userRepoWrite = new UserRepositoryWrite(game.GlobalState, game.World, TimeProvider.System);

			var user = userRepoWrite.CreateUser("ghapikey", "apiuser", "API User");
			var playerId = PlayerIdFactory.Create(Guid.NewGuid().ToString());
			game.PlayerRepositoryWrite.CreatePlayer(playerId, user.UserId);

			const string testHash = "abc123hash";
			userRepoWrite.SetApiKeyHash(playerId, testHash);

			var found = userRepo.GetPlayerByApiKeyHash(testHash);
			Assert.NotNull(found);
			Assert.Equal(playerId, found!.PlayerId);
		}

		[Fact]
		public void GetOrCreateUser_NewUser_CreatesAndReturnsUser() {
			var game = new TestGame();
			var userRepoWrite = new UserRepositoryWrite(game.GlobalState, game.World, TimeProvider.System);

			var user = userRepoWrite.GetOrCreateUser("gh-new", "newuser", "New User");

			Assert.NotNull(user);
			Assert.Equal("gh-new", user.GithubId);
			Assert.Equal("newuser", user.GithubLogin);
			Assert.Equal("New User", user.DisplayName);
		}

		[Fact]
		public void GetOrCreateUser_ExistingUser_ReturnsSameUser() {
			var game = new TestGame();
			var userRepoWrite = new UserRepositoryWrite(game.GlobalState, game.World, TimeProvider.System);

			var first = userRepoWrite.GetOrCreateUser("gh-dup", "dupuser", "Dup User");
			var second = userRepoWrite.GetOrCreateUser("gh-dup", "dupuser-2", "Dup User 2");

			Assert.Equal(first.UserId, second.UserId);
			Assert.Equal("dupuser", second.GithubLogin);
		}

		[Fact]
		public void GetOrCreateUser_ConcurrentCalls_ReturnSameUser() {
			var game = new TestGame();
			var userRepoWrite = new UserRepositoryWrite(game.GlobalState, game.World, TimeProvider.System);
			UserImmutable? result1 = null;
			UserImmutable? result2 = null;

			var t1 = new System.Threading.Thread(() => result1 = userRepoWrite.GetOrCreateUser("gh-race", "racer", "Racer"));
			var t2 = new System.Threading.Thread(() => result2 = userRepoWrite.GetOrCreateUser("gh-race", "racer", "Racer"));
			t1.Start();
			t2.Start();
			t1.Join();
			t2.Join();

			Assert.NotNull(result1);
			Assert.NotNull(result2);
			Assert.Equal(result1!.UserId, result2!.UserId);
		}

		[Fact]
		public void RevokeApiKey_ClearsHash() {
			var game = new TestGame();
			var userRepo = new UserRepository(game.GlobalState, game.Accessor);
			var userRepoWrite = new UserRepositoryWrite(game.GlobalState, game.World, TimeProvider.System);

			var user = userRepoWrite.CreateUser("ghrevoke", "revokeuser", "Revoke User");
			var playerId = PlayerIdFactory.Create(Guid.NewGuid().ToString());
			game.PlayerRepositoryWrite.CreatePlayer(playerId, user.UserId);

			userRepoWrite.SetApiKeyHash(playerId, "somehash");
			userRepoWrite.SetApiKeyHash(playerId, null);

			var found = userRepo.GetPlayerByApiKeyHash("somehash");
			Assert.Null(found);
		}
	}
}
