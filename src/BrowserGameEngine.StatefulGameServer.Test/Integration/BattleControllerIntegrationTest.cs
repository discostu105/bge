using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using BrowserGameEngine.Shared;
using Xunit;

namespace BrowserGameEngine.StatefulGameServer.Test.Integration {
	public class BattleControllerIntegrationTest : IntegrationTestBase {
		public BattleControllerIntegrationTest(BgeWebApplicationFactory factory) : base(factory) { }

		[Fact]
		public async Task AttackablePlayers_Unauthenticated_Returns401() {
			var client = CreateClient();
			var response = await client.GetAsync("/api/battle/attackableplayers");
			Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
		}

		[Fact]
		public async Task AttackablePlayers_Authenticated_ReturnsOk() {
			var userId = "user-battle-atk-1";
			await CreatePlayerAsync(userId, "BattlePlayer1");

			var client = CreateClient(userId);
			var response = await client.GetAsync("/api/battle/attackableplayers");
			Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		}

		[Fact]
		public async Task EnemyBase_Unauthenticated_Returns401() {
			var client = CreateClient();
			var response = await client.GetAsync("/api/battle/enemybase?enemyPlayerId=nonexistent");
			Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
		}

		[Fact]
		public async Task EnemyBase_WithoutSendingUnits_ReturnsBadRequest() {
			// Viewing an enemy base requires sending units first or having spy intel
			var attackerId = "user-battle-enemybase-1";
			var defenderId = "user-battle-enemybase-2";
			await CreatePlayerAsync(attackerId, "EBAttacker1");
			var defenderPlayerId = await CreatePlayerAsync(defenderId, "EBDefender1");

			var client = CreateClient(attackerId);
			var response = await client.GetAsync($"/api/battle/enemybase?enemyPlayerId={defenderPlayerId}");
			Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
		}

		[Fact]
		public async Task SendUnits_Unauthenticated_Returns401() {
			var client = CreateClient();
			var response = await client.PostAsync("/api/battle/sendunits?unitId=u1&enemyPlayerId=p1", null);
			Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
		}

		[Fact]
		public async Task Attack_Unauthenticated_Returns401() {
			var client = CreateClient();
			var response = await client.PostAsync("/api/battle/attack?enemyPlayerId=p1", null);
			Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
		}

		[Fact]
		public async Task Attack_NoUnitsSent_ReturnsBadRequest() {
			var attackerId = "user-battle-nounits-1";
			var defenderId = "user-battle-nounits-2";
			await CreatePlayerAsync(attackerId, "NoUnitsAttacker1");
			var defenderPlayerId = await CreatePlayerAsync(defenderId, "NoUnitsDefender1");

			var client = CreateClient(attackerId);
			// No units sent → attack fails with BadRequest
			var response = await client.PostAsync($"/api/battle/attack?enemyPlayerId={defenderPlayerId}", null);
			Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
		}

		[Fact]
		public async Task Reports_Unauthenticated_Returns401() {
			var client = CreateClient();
			var response = await client.GetAsync("/api/battle/reports");
			Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
		}

		[Fact]
		public async Task Reports_Authenticated_ReturnsOk() {
			var userId = "user-battle-reports-1";
			await CreatePlayerAsync(userId, "ReportsPlayer1");

			var client = CreateClient(userId);
			var response = await client.GetAsync("/api/battle/reports");
			Assert.Equal(HttpStatusCode.OK, response.StatusCode);

			var reports = await DeserializeAsync<BattleReportSummaryViewModel[]>(response);
			Assert.NotNull(reports);
			Assert.Empty(reports!);
		}

		[Fact]
		public async Task Report_Unauthenticated_Returns401() {
			var client = CreateClient();
			var response = await client.GetAsync($"/api/battle/report?reportId={Guid.NewGuid()}");
			Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
		}

		[Fact]
		public async Task Report_NonExistentReport_Returns404() {
			var userId = "user-battle-report-1";
			await CreatePlayerAsync(userId, "ReportPlayer1");

			var client = CreateClient(userId);
			var response = await client.GetAsync($"/api/battle/report?reportId={Guid.NewGuid()}");
			Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
		}
	}
}
