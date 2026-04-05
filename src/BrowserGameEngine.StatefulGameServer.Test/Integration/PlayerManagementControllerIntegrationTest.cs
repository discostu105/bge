using System.Net;
using System.Threading.Tasks;
using BrowserGameEngine.Shared;
using Xunit;

namespace BrowserGameEngine.StatefulGameServer.Test.Integration {
	public class PlayerManagementControllerIntegrationTest : IntegrationTestBase {
		public PlayerManagementControllerIntegrationTest(BgeWebApplicationFactory factory) : base(factory) { }

		[Fact]
		public async Task GetMyPlayers_Unauthenticated_Returns401() {
			var client = CreateClient();
			var response = await client.GetAsync("/api/player-management");
			Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
		}

		[Fact]
		public async Task GetMyPlayers_NewUser_ReturnsEmptyList() {
			var client = CreateClient("user-pm-new-1");
			var response = await client.GetAsync("/api/player-management");
			Assert.Equal(HttpStatusCode.OK, response.StatusCode);
			var vm = await DeserializeAsync<PlayerListViewModel>(response);
			Assert.NotNull(vm);
			Assert.Empty(vm!.Players);
		}

		[Fact]
		public async Task GetMyPlayers_AfterJoining_ReturnsPlayer() {
			var userId = "user-pm-joined-1";
			await CreatePlayerAsync(userId, "PMTestPlayer1");

			var client = CreateClient(userId);
			var response = await client.GetAsync("/api/player-management");
			Assert.Equal(HttpStatusCode.OK, response.StatusCode);
			var vm = await DeserializeAsync<PlayerListViewModel>(response);
			Assert.NotNull(vm);
			Assert.Single(vm!.Players);
			Assert.Equal("PMTestPlayer1", vm.Players[0].PlayerName);
		}

		[Fact]
		public async Task GenerateApiKey_Unauthenticated_Returns401() {
			var client = CreateClient();
			var response = await client.PostAsync("/api/player-management/some-player-id/apikey", null);
			Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
		}

		[Fact]
		public async Task GenerateApiKey_ForOwnPlayer_ReturnsToken() {
			var userId = "user-pm-apikey-1";
			var playerId = await CreatePlayerAsync(userId, "APIKeyPlayer1");

			var client = CreateClient(userId);
			var response = await client.PostAsync($"/api/player-management/{playerId}/apikey", null);
			Assert.Equal(HttpStatusCode.OK, response.StatusCode);
			var vm = await DeserializeAsync<ApiKeyViewModel>(response);
			Assert.NotNull(vm);
			Assert.False(string.IsNullOrEmpty(vm!.ApiKey));
		}

		[Fact]
		public async Task DeletePlayer_Unauthenticated_Returns401() {
			var client = CreateClient();
			var response = await client.DeleteAsync("/api/player-management/some-player-id");
			Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
		}

		[Fact]
		public async Task DeletePlayer_ActivePlayer_ReturnsConflict() {
			// When a user has only one player, it becomes the active player.
			// Deleting the active player returns 409 Conflict.
			var userId = "user-pm-delete-1";
			await CreatePlayerAsync(userId, "DeleteTestPlayer1");

			var client = CreateClient(userId);
			var listResp = await client.GetAsync("/api/player-management");
			var vm = await DeserializeAsync<PlayerListViewModel>(listResp);
			var playerId = vm!.Players[0].PlayerId;

			var response = await client.DeleteAsync($"/api/player-management/{playerId}");
			Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
		}
	}
}
