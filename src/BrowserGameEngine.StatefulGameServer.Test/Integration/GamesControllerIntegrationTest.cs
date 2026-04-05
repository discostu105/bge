using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using BrowserGameEngine.Shared;
using Xunit;

namespace BrowserGameEngine.StatefulGameServer.Test.Integration {
	public class GamesControllerIntegrationTest : IntegrationTestBase {
		public GamesControllerIntegrationTest(BgeWebApplicationFactory factory) : base(factory) { }

		[Fact]
		public async Task GetAll_Authenticated_ReturnsGameList() {
			// GetAll only needs authentication, not an active player
			var client = CreateClient("user-games-getall-1");
			var response = await client.GetAsync("/api/games");
			Assert.Equal(HttpStatusCode.OK, response.StatusCode);
			var vm = await DeserializeAsync<GameListViewModel>(response);
			Assert.NotNull(vm);
			Assert.NotEmpty(vm!.Games);
		}

		[Fact]
		public async Task GetAll_Unauthenticated_ReturnsOk() {
			// GetAll is [AllowAnonymous]
			var client = CreateClient();
			var response = await client.GetAsync("/api/games");
			Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		}

		[Fact]
		public async Task GetById_KnownGame_ReturnsDetail() {
			// GetById has [AllowAnonymous]
			var client = CreateClient();
			var response = await client.GetAsync("/api/games/default");
			Assert.Equal(HttpStatusCode.OK, response.StatusCode);
			var vm = await DeserializeAsync<GameDetailViewModel>(response);
			Assert.NotNull(vm);
			Assert.Equal("default", vm!.GameId);
		}

		[Fact]
		public async Task GetById_UnknownGame_Returns404() {
			var client = CreateClient();
			var response = await client.GetAsync("/api/games/does-not-exist-xyz");
			Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
		}

		[Fact]
		public async Task CreateGame_Unauthenticated_Returns401() {
			var client = CreateClient();
			var request = new CreateGameRequest(
				Name: "New Test Game",
				GameDefType: "sco",
				StartTime: DateTime.UtcNow.AddMinutes(1),
				EndTime: DateTime.UtcNow.AddDays(7),
				TickDuration: "00:00:10"
			);
			var response = await client.PostAsJsonAsync("/api/games", request, JsonOptions);
			Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
		}

		[Fact]
		public async Task CreateGame_Authenticated_Returns201() {
			// Creating a game requires IsValid (an active player)
			var userId = "user-create-game-2";
			await CreatePlayerAsync(userId, "GameCreatorPlayer");

			var client = CreateClient(userId);
			var request = new CreateGameRequest(
				Name: "Integration Test Game Created",
				GameDefType: "sco",
				StartTime: DateTime.UtcNow.AddMinutes(1),
				EndTime: DateTime.UtcNow.AddDays(7),
				TickDuration: "00:00:10"
			);
			var response = await client.PostAsJsonAsync("/api/games", request, JsonOptions);
			Assert.Equal(HttpStatusCode.Created, response.StatusCode);
			var vm = await DeserializeAsync<GameSummaryViewModel>(response);
			Assert.NotNull(vm);
			Assert.Equal("Integration Test Game Created", vm!.Name);
		}

		[Fact]
		public async Task JoinGame_Unauthenticated_Returns401() {
			var client = CreateClient();
			var request = new JoinGameRequest(PlayerName: "ShouldFail");
			var response = await client.PostAsJsonAsync("/api/games/default/join", request, JsonOptions);
			Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
		}

		[Fact]
		public async Task JoinGame_AuthenticatedUser_JoinsNewGame() {
			// To use the Join endpoint, user must already have a player (IsValid).
			// 1. Create player in default game to establish IsValid.
			var userId = "user-join-newgame-1";
			await CreatePlayerAsync(userId, "JoinSetupPlayer");
			var client = CreateClient(userId);

			// 2. Create a new upcoming game.
			var createReq = new CreateGameRequest(
				Name: "Joinable Game",
				GameDefType: "sco",
				StartTime: DateTime.UtcNow.AddMinutes(1),
				EndTime: DateTime.UtcNow.AddDays(7),
				TickDuration: "00:00:10"
			);
			var createResp = await client.PostAsJsonAsync("/api/games", createReq, JsonOptions);
			Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);
			var newGame = await DeserializeAsync<GameSummaryViewModel>(createResp);

			// 3. Join the new game (creates a second player for this user in the new game).
			var joinReq = new JoinGameRequest(PlayerName: "JoinedPlayer");
			var joinResp = await client.PostAsJsonAsync($"/api/games/{newGame!.GameId}/join", joinReq, JsonOptions);
			Assert.Equal(HttpStatusCode.OK, joinResp.StatusCode);
			var body = await joinResp.Content.ReadAsStringAsync();
			Assert.Contains("playerId", body, StringComparison.OrdinalIgnoreCase);
		}
	}
}
