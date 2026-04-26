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
			var userRepo = new UserRepository(game.GlobalState, game.World);
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
			var userRepo = new UserRepository(game.GlobalState, game.World);
			var userRepoWrite = new UserRepositoryWrite(game.GlobalState, game.World, TimeProvider.System);

			userRepoWrite.CreateUser("gh456", "monalisa", "Mona Lisa");

			var found = userRepo.GetByGithubId("gh456");
			Assert.NotNull(found);
			Assert.Equal("monalisa", found!.GithubLogin);
		}

		[Fact]
		public void GetByGithubId_MissingUser_ReturnsNull() {
			var game = new TestGame();
			var userRepo = new UserRepository(game.GlobalState, game.World);

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
			var userRepo = new UserRepository(game.GlobalState, game.World);
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
		public void AddApiKey_AndLookup_FindsPlayer() {
			var game = new TestGame();
			var userRepo = new UserRepository(game.GlobalState, game.World);
			var userRepoWrite = new UserRepositoryWrite(game.GlobalState, game.World, TimeProvider.System);

			var user = userRepoWrite.CreateUser("ghapikey", "apiuser", "API User");
			var playerId = PlayerIdFactory.Create(Guid.NewGuid().ToString());
			game.PlayerRepositoryWrite.CreatePlayer(playerId, user.UserId);

			const string testHash = "abc123hash";
			var added = userRepoWrite.AddApiKey(playerId, testHash, "bge_k_abc12345", "primary");

			Assert.NotNull(added);
			Assert.Equal("primary", added.Name);
			Assert.Equal("bge_k_abc12345", added.KeyPrefix);

			var found = userRepo.GetPlayerByApiKeyHash(testHash);
			Assert.NotNull(found);
			Assert.Equal(playerId, found!.PlayerId);
			Assert.Single(found.ApiKeys!);
		}

		[Fact]
		public void AddApiKey_MultipleKeys_AllResolveToSamePlayer() {
			var game = new TestGame();
			var userRepo = new UserRepository(game.GlobalState, game.World);
			var userRepoWrite = new UserRepositoryWrite(game.GlobalState, game.World, TimeProvider.System);

			var user = userRepoWrite.CreateUser("ghmultikey", "multi", "Multi");
			var playerId = PlayerIdFactory.Create(Guid.NewGuid().ToString());
			game.PlayerRepositoryWrite.CreatePlayer(playerId, user.UserId);

			userRepoWrite.AddApiKey(playerId, "hash-a", "bge_k_aaaa", "key-a");
			userRepoWrite.AddApiKey(playerId, "hash-b", "bge_k_bbbb", "key-b");

			Assert.Equal(playerId, userRepo.GetPlayerByApiKeyHash("hash-a")!.PlayerId);
			Assert.Equal(playerId, userRepo.GetPlayerByApiKeyHash("hash-b")!.PlayerId);
			Assert.Equal(2, userRepo.GetApiKeys(playerId).Count());
		}

		[Fact]
		public void TouchApiKey_UpdatesLastAccessedAt() {
			var game = new TestGame();
			var userRepo = new UserRepository(game.GlobalState, game.World);
			var userRepoWrite = new UserRepositoryWrite(game.GlobalState, game.World, TimeProvider.System);

			var user = userRepoWrite.CreateUser("ghtouch", "touchuser", "Touch User");
			var playerId = PlayerIdFactory.Create(Guid.NewGuid().ToString());
			game.PlayerRepositoryWrite.CreatePlayer(playerId, user.UserId);

			userRepoWrite.AddApiKey(playerId, "touch-hash", "bge_k_touch123", null);
			Assert.Null(userRepo.GetApiKeys(playerId).Single().LastAccessedAt);

			userRepoWrite.TouchApiKey("touch-hash");
			Assert.NotNull(userRepo.GetApiKeys(playerId).Single().LastAccessedAt);
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
		public void RemoveApiKey_RemovesOnlyTheTargetedKey() {
			var game = new TestGame();
			var userRepo = new UserRepository(game.GlobalState, game.World);
			var userRepoWrite = new UserRepositoryWrite(game.GlobalState, game.World, TimeProvider.System);

			var user = userRepoWrite.CreateUser("ghrevoke", "revokeuser", "Revoke User");
			var playerId = PlayerIdFactory.Create(Guid.NewGuid().ToString());
			game.PlayerRepositoryWrite.CreatePlayer(playerId, user.UserId);

			var key1 = userRepoWrite.AddApiKey(playerId, "hash-1", "bge_k_1111", "k1");
			var key2 = userRepoWrite.AddApiKey(playerId, "hash-2", "bge_k_2222", "k2");

			var removed = userRepoWrite.RemoveApiKey(playerId, key1.KeyId);

			Assert.True(removed);
			Assert.Null(userRepo.GetPlayerByApiKeyHash("hash-1"));
			Assert.NotNull(userRepo.GetPlayerByApiKeyHash("hash-2"));
			Assert.Single(userRepo.GetApiKeys(playerId));
		}

		[Fact]
		public void RemoveApiKey_UnknownKey_ReturnsFalse() {
			var game = new TestGame();
			var userRepoWrite = new UserRepositoryWrite(game.GlobalState, game.World, TimeProvider.System);

			var user = userRepoWrite.CreateUser("ghrevoke2", "revokeuser2", "Revoke2");
			var playerId = PlayerIdFactory.Create(Guid.NewGuid().ToString());
			game.PlayerRepositoryWrite.CreatePlayer(playerId, user.UserId);

			var removed = userRepoWrite.RemoveApiKey(playerId, "nonexistent");
			Assert.False(removed);
		}
	}
}
