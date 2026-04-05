using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using BrowserGameEngine.Shared;
using Xunit;

namespace BrowserGameEngine.StatefulGameServer.Test.Integration {
	/// <summary>
	/// Base class for BGE controller integration tests. All test methods within a class
	/// share one <see cref="BgeWebApplicationFactory"/> instance (fast startup) so tests
	/// must use unique identifiers to avoid cross-test state interference.
	/// </summary>
	[Collection("Integration")]
	public abstract class IntegrationTestBase {
		protected readonly BgeWebApplicationFactory Factory;

		protected static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web) {
			DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		};

		protected IntegrationTestBase(BgeWebApplicationFactory factory) {
			Factory = factory;
		}

		protected HttpClient CreateClient() => Factory.CreateClient();

		protected HttpClient CreateClient(string userId) => Factory.CreateAuthenticatedClient(userId);

		/// <summary>
		/// Creates a player in the default game for the given userId.
		/// Uses POST /api/players which only requires UserId (not an existing player).
		/// Returns the created PlayerId string.
		/// </summary>
		protected async Task<string> CreatePlayerAsync(string userId, string playerName) {
			var client = CreateClient(userId);
			var request = new CreatePlayerForUserViewModel { PlayerName = playerName };
			var response = await client.PostAsJsonAsync("/api/players", request, JsonOptions);
			response.EnsureSuccessStatusCode();
			var vm = await DeserializeAsync<PlayerSummaryViewModel>(response);
			return vm!.PlayerId;
		}

		protected async Task<T?> DeserializeAsync<T>(HttpResponseMessage response) {
			var content = await response.Content.ReadAsStringAsync();
			return JsonSerializer.Deserialize<T>(content, JsonOptions);
		}
	}
}
