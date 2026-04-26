using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.Shared;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using GameRegistryNs = BrowserGameEngine.StatefulGameServer.GameRegistry;

namespace BrowserGameEngine.StatefulGameServer.Test.Integration {
	/// <summary>
	/// Verifies that GET /api/resources scoped via the X-Game-Id header returns
	/// each game's own resources. Regression test for the bug where every game
	/// URL showed identical data because all repositories read the singleton-bound
	/// default WorldState.
	/// </summary>
	public class MultiGameIsolationIntegrationTest : IntegrationTestBase {
		public MultiGameIsolationIntegrationTest(BgeWebApplicationFactory factory) : base(factory) { }

		private record JoinResult([property: JsonPropertyName("playerId")] string PlayerId);

		[Fact]
		public async Task Resources_AreScopedByGameIdHeader() {
			var userId = $"iso-user-{Guid.NewGuid():N}";

			// Bootstrap: create a player in the default game so this user is "valid"
			// for the cookie path on subsequent calls (CreateGame requires IsValid).
			await CreatePlayerAsync(userId, "BootPlayer");

			var gameAId = await CreateGameAsync(userId, "iso-A");
			var gameBId = await CreateGameAsync(userId, "iso-B");

			var playerAId = await JoinGameAsync(userId, gameAId, "Alice-A");
			var playerBId = await JoinGameAsync(userId, gameBId, "Alice-B");

			// Baseline before mutation — both games seed with the same starting minerals.
			var baselineA = await GetMineralsViaApiAsync(userId, gameAId);
			var baselineB = await GetMineralsViaApiAsync(userId, gameBId);
			Assert.Equal(baselineA, baselineB);

			AddMinerals(gameAId, playerAId, 1234);
			AddMinerals(gameBId, playerBId, 5678);

			var minA = await GetMineralsViaApiAsync(userId, gameAId);
			var minB = await GetMineralsViaApiAsync(userId, gameBId);

			Assert.Equal(baselineA + 1234m, minA);
			Assert.Equal(baselineB + 5678m, minB);
			Assert.NotEqual(minA, minB); // isolation proven
		}

		[Fact]
		public async Task UnknownGameIdHeader_Returns400() {
			var userId = $"iso-unknown-{Guid.NewGuid():N}";
			await CreatePlayerAsync(userId, "BootPlayer");

			var client = CreateClient(userId);
			client.DefaultRequestHeaders.Add("X-Game-Id", "this-game-does-not-exist");

			var response = await client.GetAsync("/api/resources");
			Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
		}

		[Fact]
		public async Task CookieLogin_SelectsPlayerInCurrentGame() {
			var userId = $"iso-cookie-{Guid.NewGuid():N}";

			await CreatePlayerAsync(userId, "BootPlayer");

			var gameAId = await CreateGameAsync(userId, "cookie-A");
			var gameBId = await CreateGameAsync(userId, "cookie-B");

			var playerAId = await JoinGameAsync(userId, gameAId, "Alice-A");
			var playerBId = await JoinGameAsync(userId, gameBId, "Alice-B");

			var baselineA = await GetMineralsViaApiAsync(userId, gameAId);
			var baselineB = await GetMineralsViaApiAsync(userId, gameBId);

			AddMinerals(gameAId, playerAId, 11);
			AddMinerals(gameBId, playerBId, 22);

			Assert.Equal(baselineA + 11m, await GetMineralsViaApiAsync(userId, gameAId));
			Assert.Equal(baselineB + 22m, await GetMineralsViaApiAsync(userId, gameBId));
		}

		// --- helpers ---

		private async Task<string> CreateGameAsync(string userId, string namePrefix) {
			var client = CreateClient(userId);
			var request = new CreateGameRequest(
				Name: $"{namePrefix}-{Guid.NewGuid():N}",
				GameDefType: "sco",
				StartTime: DateTime.UtcNow.AddMinutes(-5),
				EndTime: DateTime.UtcNow.AddDays(1),
				TickDuration: "00:00:10",
				MaxPlayers: 8);
			var resp = await client.PostAsJsonAsync("/api/games", request, JsonOptions);
			resp.EnsureSuccessStatusCode();
			var summary = await DeserializeAsync<GameSummaryViewModel>(resp);
			Assert.NotNull(summary);
			return summary!.GameId;
		}

		private async Task<string> JoinGameAsync(string userId, string gameId, string playerName) {
			var client = CreateClient(userId);
			var request = new JoinGameRequest(PlayerName: playerName);
			var resp = await client.PostAsJsonAsync($"/api/games/{gameId}/join", request, JsonOptions);
			resp.EnsureSuccessStatusCode();
			var join = await DeserializeAsync<JoinResult>(resp);
			Assert.NotNull(join);
			return join!.PlayerId;
		}

		private void AddMinerals(string gameId, string playerId, decimal amount) {
			var registry = Factory.Services.GetRequiredService<GameRegistryNs.GameRegistry>();
			var instance = registry.TryGetInstance(new GameId(gameId));
			Assert.NotNull(instance);

			var resourceRepository = new ResourceRepository(instance!.WorldStateAccessor, instance.GameDef);
			var write = new ResourceRepositoryWrite(instance.WorldStateAccessor, resourceRepository, instance.GameDef);
			write.AddResources(PlayerIdFactory.Create(playerId), Id.ResDef("minerals"), amount);
		}

		private async Task<decimal> GetMineralsViaApiAsync(string userId, string gameId) {
			var client = CreateClient(userId);
			client.DefaultRequestHeaders.Add("X-Game-Id", gameId);
			var resp = await client.GetAsync("/api/resources");
			resp.EnsureSuccessStatusCode();
			var vm = await DeserializeAsync<PlayerResourcesViewModel>(resp);
			Assert.NotNull(vm);
			vm!.SecondaryResources.Cost.TryGetValue("minerals", out var v);
			return v;
		}
	}
}
