using System.Net;
using System.Net.Http.Json;
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
			Assert.Equal(0, vm.Players[0].ApiKeyCount);
		}

		[Fact]
		public async Task CreateApiKey_Unauthenticated_Returns401() {
			var client = CreateClient();
			var response = await client.PostAsync("/api/player-management/some-player-id/apikeys", null);
			Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
		}

		[Fact]
		public async Task CreateApiKey_ForOwnPlayer_ReturnsTokenAndMetadata() {
			var userId = "user-pm-apikey-1";
			var playerId = await CreatePlayerAsync(userId, "APIKeyPlayer1");

			var client = CreateClient(userId);
			var response = await client.PostAsJsonAsync(
				$"/api/player-management/{playerId}/apikeys",
				new CreateApiKeyRequest { Name = "my-bot" });
			Assert.Equal(HttpStatusCode.OK, response.StatusCode);
			var vm = await DeserializeAsync<CreateApiKeyResponse>(response);
			Assert.NotNull(vm);
			Assert.False(string.IsNullOrEmpty(vm!.ApiKey));
			Assert.False(string.IsNullOrEmpty(vm.KeyId));
			Assert.Equal("my-bot", vm.Name);
			Assert.StartsWith("bge_k_", vm.KeyPrefix);
		}

		[Fact]
		public async Task ListApiKeys_AfterCreate_IncludesKey() {
			var userId = "user-pm-apikey-list-1";
			var playerId = await CreatePlayerAsync(userId, "ListKeyPlayer1");

			var client = CreateClient(userId);
			await client.PostAsJsonAsync($"/api/player-management/{playerId}/apikeys",
				new CreateApiKeyRequest { Name = "first" });
			await client.PostAsJsonAsync($"/api/player-management/{playerId}/apikeys",
				new CreateApiKeyRequest { Name = "second" });

			var listResp = await client.GetAsync($"/api/player-management/{playerId}/apikeys");
			Assert.Equal(HttpStatusCode.OK, listResp.StatusCode);
			var list = await DeserializeAsync<ApiKeyListViewModel>(listResp);
			Assert.NotNull(list);
			Assert.Equal(2, list!.Keys.Count);
			Assert.Contains(list.Keys, k => k.Name == "first");
			Assert.Contains(list.Keys, k => k.Name == "second");
		}

		[Fact]
		public async Task RevokeApiKey_RemovesOnlyTargetedKey() {
			var userId = "user-pm-revoke-1";
			var playerId = await CreatePlayerAsync(userId, "RevokeKeyPlayer1");

			var client = CreateClient(userId);
			var first = await (await client.PostAsJsonAsync($"/api/player-management/{playerId}/apikeys",
				new CreateApiKeyRequest { Name = "keep" })).Content.ReadFromJsonAsync<CreateApiKeyResponse>();
			var second = await (await client.PostAsJsonAsync($"/api/player-management/{playerId}/apikeys",
				new CreateApiKeyRequest { Name = "revoke-me" })).Content.ReadFromJsonAsync<CreateApiKeyResponse>();

			var revokeResp = await client.DeleteAsync($"/api/player-management/{playerId}/apikeys/{second!.KeyId}");
			Assert.Equal(HttpStatusCode.NoContent, revokeResp.StatusCode);

			var list = await DeserializeAsync<ApiKeyListViewModel>(
				await client.GetAsync($"/api/player-management/{playerId}/apikeys"));
			Assert.Single(list!.Keys);
			Assert.Equal(first!.KeyId, list.Keys[0].KeyId);
		}

		[Fact]
		public async Task RevokeApiKey_UnknownKey_Returns404() {
			var userId = "user-pm-revoke-404";
			var playerId = await CreatePlayerAsync(userId, "Revoke404");

			var client = CreateClient(userId);
			var response = await client.DeleteAsync($"/api/player-management/{playerId}/apikeys/nonexistent-key-id");
			Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
		}

		[Fact]
		public async Task CreateApiKey_ThenUseAsBearer_AuthenticatesAndUpdatesLastAccessed() {
			var userId = "user-pm-apikey-bearer-1";
			var playerId = await CreatePlayerAsync(userId, "BearerPlayer1");

			var authClient = CreateClient(userId);
			var genResponse = await authClient.PostAsJsonAsync(
				$"/api/player-management/{playerId}/apikeys",
				new CreateApiKeyRequest { Name = "bot" });
			Assert.Equal(HttpStatusCode.OK, genResponse.StatusCode);
			var vm = await DeserializeAsync<CreateApiKeyResponse>(genResponse);
			Assert.NotNull(vm);

			var bearerClient = CreateClient();
			bearerClient.DefaultRequestHeaders.Authorization =
				new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", vm!.ApiKey);

			var response = await bearerClient.GetAsync("/api/profile");
			Assert.Equal(HttpStatusCode.OK, response.StatusCode);

			var listAfter = await DeserializeAsync<ApiKeyListViewModel>(
				await authClient.GetAsync($"/api/player-management/{playerId}/apikeys"));
			Assert.NotNull(listAfter!.Keys[0].LastAccessedAt);
		}

		[Fact]
		public async Task BearerToken_InvalidKey_Returns401() {
			var bearerClient = CreateClient();
			bearerClient.DefaultRequestHeaders.Authorization =
				new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "bge_k_invalid_key_here");

			var response = await bearerClient.GetAsync("/api/profile");
			Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
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
