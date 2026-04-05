using System;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using BrowserGameEngine.Shared;
using Xunit;

namespace BrowserGameEngine.StatefulGameServer.Test.Integration {
	/// <summary>
	/// End-to-end integration tests covering the critical game flows:
	/// player registration, joining a game, building assets, training units, and combat.
	/// All tests use unique user/player IDs to avoid cross-test state interference.
	/// </summary>
	public class CriticalGameFlowsIntegrationTest : IntegrationTestBase {
		public CriticalGameFlowsIntegrationTest(BgeWebApplicationFactory factory) : base(factory) { }

		// ─── Player Registration ───────────────────────────────────────────────

		[Fact]
		public async Task PlayerRegistration_NewUser_CanRegisterAndSeePlayer() {
			var userId = "e2e-reg-1";
			var playerId = await CreatePlayerAsync(userId, "RegistrationHero");

			var client = CreateClient(userId);
			var response = await client.GetAsync("/api/player-management");
			Assert.Equal(HttpStatusCode.OK, response.StatusCode);

			var vm = await DeserializeAsync<PlayerListViewModel>(response);
			Assert.NotNull(vm);
			Assert.Single(vm!.Players);
			Assert.Equal("RegistrationHero", vm.Players[0].PlayerName);
			Assert.Equal(playerId, vm.Players[0].PlayerId);
		}

		[Fact]
		public async Task PlayerRegistration_EmptyName_ReturnsBadRequest() {
			var client = CreateClient("e2e-reg-emptyname-1");
			var request = new CreatePlayerForUserViewModel { PlayerName = "" };
			var response = await client.PostAsJsonAsync("/api/player-management", request, JsonOptions);
			Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
		}

		[Fact]
		public async Task PlayerRegistration_Unauthenticated_Returns401() {
			var client = CreateClient();
			var request = new CreatePlayerForUserViewModel { PlayerName = "ShouldNotWork" };
			var response = await client.PostAsJsonAsync("/api/player-management", request, JsonOptions);
			Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
		}

		// ─── Joining a Game (player gets initial state) ────────────────────────

		[Fact]
		public async Task JoinGame_NewPlayer_GetsResources() {
			var userId = "e2e-join-res-1";
			await CreatePlayerAsync(userId, "ResourceChecker");

			var client = CreateClient(userId);
			var response = await client.GetAsync("/api/resources/get");
			Assert.Equal(HttpStatusCode.OK, response.StatusCode);

			var vm = await DeserializeAsync<PlayerResourcesViewModel>(response);
			Assert.NotNull(vm);
			Assert.NotNull(vm!.PrimaryResource);
		}

		[Fact]
		public async Task JoinGame_NewPlayer_HasCommandCenter() {
			var userId = "e2e-join-assets-1";
			await CreatePlayerAsync(userId, "AssetChecker");

			var client = CreateClient(userId);
			var response = await client.GetAsync("/api/assets/get");
			Assert.Equal(HttpStatusCode.OK, response.StatusCode);

			var vm = await DeserializeAsync<AssetsViewModel>(response);
			Assert.NotNull(vm);
			// New Terran player starts with a Command Center already built.
			Assert.Contains(vm!.Assets, a => a.Built && a.Definition.Id == "commandcenter");
		}

		[Fact]
		public async Task JoinGame_NewPlayer_UnitsListIsEmpty() {
			// New players are created with no pre-built units — they must train units themselves.
			var userId = "e2e-join-units-1";
			await CreatePlayerAsync(userId, "UnitChecker");

			var client = CreateClient(userId);
			var response = await client.GetAsync("/api/units/get");
			Assert.Equal(HttpStatusCode.OK, response.StatusCode);

			var vm = await DeserializeAsync<UnitsViewModel>(response);
			Assert.NotNull(vm);
			Assert.Empty(vm!.Units);
		}

		[Fact]
		public async Task JoinGame_Unauthenticated_Returns401() {
			var client = CreateClient();
			var request = new JoinGameRequest(PlayerName: "ShouldFail");
			var response = await client.PostAsJsonAsync("/api/games/default/join", request, JsonOptions);
			Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
		}

		// ─── Building Assets ───────────────────────────────────────────────────

		[Fact]
		public async Task BuildAsset_ValidAsset_Returns200() {
			var userId = "e2e-build-asset-1";
			await CreatePlayerAsync(userId, "BuilderPlayer1");

			var client = CreateClient(userId);
			// Terran players start with commandcenter; barracks requires commandcenter.
			var response = await client.PostAsync("/api/assets/build?assetDefId=barracks", null);
			Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		}

		[Fact]
		public async Task BuildAsset_AlreadyBuilt_ReturnsBadRequest() {
			var userId = "e2e-build-asset-dup-1";
			await CreatePlayerAsync(userId, "BuilderDupPlayer1");

			var client = CreateClient(userId);
			// commandcenter is already built for new Terran players.
			var response = await client.PostAsync("/api/assets/build?assetDefId=commandcenter", null);
			Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
		}

		[Fact]
		public async Task BuildAsset_PrerequisiteNotMet_ReturnsBadRequest() {
			var userId = "e2e-build-prereq-1";
			await CreatePlayerAsync(userId, "PrereqPlayer1");

			var client = CreateClient(userId);
			// academy requires barracks which isn't built yet for new Terran players.
			var response = await client.PostAsync("/api/assets/build?assetDefId=academy", null);
			Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
		}

		[Fact]
		public async Task BuildAsset_Unauthenticated_Returns401() {
			var client = CreateClient();
			var response = await client.PostAsync("/api/assets/build?assetDefId=barracks", null);
			Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
		}

		[Fact]
		public async Task BuildAsset_AfterQueuing_AppearsAsQueued() {
			var userId = "e2e-build-queued-1";
			await CreatePlayerAsync(userId, "QueuedBuildPlayer1");

			var client = CreateClient(userId);
			var buildResp = await client.PostAsync("/api/assets/build?assetDefId=barracks", null);
			Assert.Equal(HttpStatusCode.OK, buildResp.StatusCode);

			// After queuing, barracks should appear in the assets list as AlreadyQueued = true.
			var assetsResp = await client.GetAsync("/api/assets/get");
			Assert.Equal(HttpStatusCode.OK, assetsResp.StatusCode);
			var vm = await DeserializeAsync<AssetsViewModel>(assetsResp);
			Assert.NotNull(vm);
			var barracks = vm!.Assets.Find(a => a.Definition.Id == "barracks");
			Assert.NotNull(barracks);
			Assert.True(barracks!.AlreadyQueued, "barracks should be queued after a build request");
		}

		// ─── Training Units ────────────────────────────────────────────────────

		[Fact]
		public async Task TrainUnits_ValidUnit_Returns200() {
			var userId = "e2e-train-1";
			await CreatePlayerAsync(userId, "TrainPlayer1");

			var client = CreateClient(userId);
			// WBF only requires commandcenter which is already built for new Terran players.
			var trainResp = await client.PostAsync("/api/units/build?unitDefId=wbf&count=5", null);
			Assert.Equal(HttpStatusCode.OK, trainResp.StatusCode);
		}

		[Fact]
		public async Task TrainUnits_InvalidUnitDef_ReturnsBadRequest() {
			var userId = "e2e-train-invalid-1";
			await CreatePlayerAsync(userId, "InvalidTrainPlayer1");

			var client = CreateClient(userId);
			var response = await client.PostAsync("/api/units/build?unitDefId=nonexistent-unit-xyz&count=1", null);
			Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
		}

		[Fact]
		public async Task TrainUnits_Unauthenticated_Returns401() {
			var client = CreateClient();
			var response = await client.PostAsync("/api/units/build?unitDefId=spacemarine&count=1", null);
			Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
		}

		[Fact]
		public async Task TrainUnits_IncreasesUnitCount() {
			var userId = "e2e-train-count-1";
			await CreatePlayerAsync(userId, "TrainCountPlayer1");

			var client = CreateClient(userId);
			var beforeResp = await client.GetAsync("/api/units/get");
			var beforeVm = await DeserializeAsync<UnitsViewModel>(beforeResp);
			int wbfBefore = beforeVm!.Units.FindAll(u => u.Definition.Id == "wbf").Sum(u => u.Count);

			var trainResp = await client.PostAsync("/api/units/build?unitDefId=wbf&count=3", null);
			Assert.Equal(HttpStatusCode.OK, trainResp.StatusCode);

			var afterResp = await client.GetAsync("/api/units/get");
			var afterVm = await DeserializeAsync<UnitsViewModel>(afterResp);
			int wbfAfter = afterVm!.Units.FindAll(u => u.Definition.Id == "wbf").Sum(u => u.Count);

			Assert.Equal(wbfBefore + 3, wbfAfter);
		}

		[Fact]
		public async Task TrainUnits_CannotAfford_ReturnsBadRequest() {
			var userId = "e2e-train-noafford-1";
			await CreatePlayerAsync(userId, "BrokePlayer1");

			var client = CreateClient(userId);
			// Request far more units than resources allow.
			var response = await client.PostAsync("/api/units/build?unitDefId=wbf&count=999999", null);
			Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
		}

		// ─── Combat ───────────────────────────────────────────────────────────

		[Fact]
		public async Task Combat_AttackablePlayers_ProtectedPlayersNotAttackable() {
			// Newly created players have 480 ticks of protection; they cannot attack or be attacked.
			var attackerId = "e2e-combat-atk-1";
			var defenderId = "e2e-combat-def-1";
			await CreatePlayerAsync(attackerId, "CombatAttacker1");
			await CreatePlayerAsync(defenderId, "CombatDefender1");

			var client = CreateClient(attackerId);
			var response = await client.GetAsync("/api/battle/attackableplayers");
			Assert.Equal(HttpStatusCode.OK, response.StatusCode);

			var vm = await DeserializeAsync<SelectEnemyViewModel>(response);
			Assert.NotNull(vm);
			// Both players are protected — neither shows up as attackable.
			Assert.DoesNotContain(vm!.AttackablePlayers, p => p.PlayerName == "CombatDefender1");
		}

		[Fact]
		public async Task Combat_AttackablePlayers_Unauthenticated_Returns401() {
			var client = CreateClient();
			var response = await client.GetAsync("/api/battle/attackableplayers");
			Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
		}

		[Fact]
		public async Task Combat_SendUnits_ProtectedDefender_ReturnsBadRequest() {
			// Players start with 480 ticks of protection. Sending units to a protected player is rejected.
			var attackerId = "e2e-sendunits-1";
			var defenderId = "e2e-sendunits-def-1";
			await CreatePlayerAsync(attackerId, "SendUnitsAttacker1");
			var defenderPlayerId = await CreatePlayerAsync(defenderId, "SendUnitsDefender1");

			// First, train a WBF so the attacker has a unit to send.
			var client = CreateClient(attackerId);
			var trainResp = await client.PostAsync("/api/units/build?unitDefId=wbf&count=1", null);
			Assert.Equal(HttpStatusCode.OK, trainResp.StatusCode);

			var unitsResp = await client.GetAsync("/api/units/get");
			var unitsVm = await DeserializeAsync<UnitsViewModel>(unitsResp);
			var wbf = unitsVm!.Units.Find(u => u.Definition.Id == "wbf");
			Assert.NotNull(wbf);

			// Sending to a protected player must fail.
			var sendResp = await client.PostAsync(
				$"/api/battle/sendunits?unitId={wbf!.UnitId}&enemyPlayerId={defenderPlayerId}",
				null);
			Assert.Equal(HttpStatusCode.BadRequest, sendResp.StatusCode);
		}

		[Fact]
		public async Task Combat_SendUnits_InvalidUnit_ReturnsBadRequest() {
			var attackerId = "e2e-sendunits-invalid-1";
			var defenderId = "e2e-sendunits-invalid-def-1";
			await CreatePlayerAsync(attackerId, "SendUnitsInvalidAttacker1");
			var defenderPlayerId = await CreatePlayerAsync(defenderId, "SendUnitsInvalidDefender1");

			var client = CreateClient(attackerId);
			var sendResp = await client.PostAsync(
				$"/api/battle/sendunits?unitId={Guid.NewGuid()}&enemyPlayerId={defenderPlayerId}",
				null);
			Assert.Equal(HttpStatusCode.BadRequest, sendResp.StatusCode);
		}

		[Fact]
		public async Task Combat_Attack_NoSentUnits_ReturnsBadRequest() {
			var attackerId = "e2e-noattack-1";
			var defenderId = "e2e-noattack-def-1";
			await CreatePlayerAsync(attackerId, "NoAttackAttacker1");
			var defenderPlayerId = await CreatePlayerAsync(defenderId, "NoAttackDefender1");

			var client = CreateClient(attackerId);
			// No units sent; attack must fail.
			var attackResp = await client.PostAsync(
				$"/api/battle/attack?enemyPlayerId={defenderPlayerId}",
				null);
			Assert.Equal(HttpStatusCode.BadRequest, attackResp.StatusCode);
		}

		[Fact]
		public async Task Combat_Attack_Unauthenticated_Returns401() {
			var client = CreateClient();
			var response = await client.PostAsync("/api/battle/attack?enemyPlayerId=someone", null);
			Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
		}
	}
}
