using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using BrowserGameEngine.Shared;
using Xunit;

namespace BrowserGameEngine.StatefulGameServer.Test.Integration {
	public class MarketControllerIntegrationTest : IntegrationTestBase {
		public MarketControllerIntegrationTest(BgeWebApplicationFactory factory) : base(factory) { }

		[Fact]
		public async Task Get_Unauthenticated_Returns401() {
			var client = CreateClient();
			var response = await client.GetAsync("/api/market/get");
			Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
		}

		[Fact]
		public async Task Get_Authenticated_ReturnsMarket() {
			var userId = "user-market-get-1";
			await CreatePlayerAsync(userId, "MarketPlayer1");

			var client = CreateClient(userId);
			var response = await client.GetAsync("/api/market/get");
			Assert.Equal(HttpStatusCode.OK, response.StatusCode);
			var vm = await DeserializeAsync<MarketViewModel>(response);
			Assert.NotNull(vm);
			Assert.NotNull(vm!.OpenOrders);
		}

		[Fact]
		public async Task Post_Unauthenticated_Returns401() {
			var client = CreateClient();
			var request = new CreateMarketOrderRequest {
				OfferedResourceId = "minerals",
				OfferedAmount = 100,
				WantedResourceId = "gas",
				WantedAmount = 50,
			};
			var response = await client.PostAsJsonAsync("/api/market/post", request, JsonOptions);
			Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
		}

		[Fact]
		public async Task Post_InvalidAmounts_ReturnsBadRequest() {
			var userId = "user-market-invalid-1";
			await CreatePlayerAsync(userId, "MarketInvalidPlayer1");

			var client = CreateClient(userId);
			var request = new CreateMarketOrderRequest {
				OfferedResourceId = "minerals",
				OfferedAmount = 0, // invalid
				WantedResourceId = "gas",
				WantedAmount = 50,
			};
			var response = await client.PostAsJsonAsync("/api/market/post", request, JsonOptions);
			Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
		}

		[Fact]
		public async Task Cancel_Unauthenticated_Returns401() {
			var client = CreateClient();
			var response = await client.DeleteAsync($"/api/market/cancel?orderId={Guid.NewGuid()}");
			Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
		}

		[Fact]
		public async Task Accept_Unauthenticated_Returns401() {
			var client = CreateClient();
			var response = await client.PostAsync($"/api/market/accept?orderId={Guid.NewGuid()}", null);
			Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
		}
	}
}
