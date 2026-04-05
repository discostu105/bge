using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using BrowserGameEngine.Shared;
using Xunit;

namespace BrowserGameEngine.StatefulGameServer.Test.Integration {
	public class AlliancesControllerIntegrationTest : IntegrationTestBase {
		public AlliancesControllerIntegrationTest(BgeWebApplicationFactory factory) : base(factory) { }

		[Fact]
		public async Task GetAll_Unauthenticated_Returns401() {
			var client = CreateClient();
			var response = await client.GetAsync("/api/alliances");
			Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
		}

		[Fact]
		public async Task GetAll_Authenticated_ReturnsOk() {
			var userId = "user-alliance-list-1";
			await CreatePlayerAsync(userId, "AllianceListPlayer1");

			var client = CreateClient(userId);
			var response = await client.GetAsync("/api/alliances");
			Assert.Equal(HttpStatusCode.OK, response.StatusCode);
			var vms = await DeserializeAsync<IEnumerable<AllianceViewModel>>(response);
			Assert.NotNull(vms);
		}

		[Fact]
		public async Task Create_Unauthenticated_Returns401() {
			var client = CreateClient();
			var request = new CreateAllianceRequest { AllianceName = "TestAlliance", Password = "secret" };
			var response = await client.PostAsJsonAsync("/api/alliances", request, JsonOptions);
			Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
		}

		[Fact]
		public async Task Create_Authenticated_ReturnsAllianceId() {
			var userId = "user-alliance-create-1";
			await CreatePlayerAsync(userId, "AllianceCreator1");

			var client = CreateClient(userId);
			var request = new CreateAllianceRequest { AllianceName = "IntegrationAlliance1", Password = "pw123" };
			var response = await client.PostAsJsonAsync("/api/alliances", request, JsonOptions);
			Assert.Equal(HttpStatusCode.OK, response.StatusCode);
			var allianceId = await response.Content.ReadAsStringAsync();
			Assert.False(string.IsNullOrEmpty(allianceId.Trim('"')));
		}

		[Fact]
		public async Task MyStatus_Unauthenticated_Returns401() {
			var client = CreateClient();
			var response = await client.GetAsync("/api/alliances/my-status");
			Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
		}

		[Fact]
		public async Task MyStatus_AfterCreating_ShowsLeaderMembership() {
			var userId = "user-alliance-status-1";
			await CreatePlayerAsync(userId, "AllianceStatusPlayer1");

			var client = CreateClient(userId);
			var request = new CreateAllianceRequest { AllianceName = "StatusAlliance1", Password = "pw" };
			await client.PostAsJsonAsync("/api/alliances", request, JsonOptions);

			var response = await client.GetAsync("/api/alliances/my-status");
			Assert.Equal(HttpStatusCode.OK, response.StatusCode);
			var vm = await DeserializeAsync<MyAllianceStatusViewModel>(response);
			Assert.NotNull(vm);
			Assert.True(vm!.IsMember);
			Assert.True(vm.IsLeader);
		}

		[Fact]
		public async Task Leave_Unauthenticated_Returns401() {
			var client = CreateClient();
			var response = await client.DeleteAsync("/api/alliances/leave");
			Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
		}

		[Fact]
		public async Task InviteAndAccept_TwoPlayers_JoinAlliance() {
			var leaderId = "user-alliance-invite-1";
			var memberId = "user-alliance-invite-2";
			var leaderPlayerId = await CreatePlayerAsync(leaderId, "AllianceLeader1");
			var memberPlayerId = await CreatePlayerAsync(memberId, "AllianceMember1");

			var leaderClient = CreateClient(leaderId);
			var memberClient = CreateClient(memberId);

			// Leader creates alliance
			var createReq = new CreateAllianceRequest { AllianceName = "InviteAlliance1", Password = "inv" };
			var createResp = await leaderClient.PostAsJsonAsync("/api/alliances", createReq, JsonOptions);
			Assert.Equal(HttpStatusCode.OK, createResp.StatusCode);
			var allianceId = (await createResp.Content.ReadAsStringAsync()).Trim('"');

			// Leader invites member
			var inviteReq = new InvitePlayerRequest { TargetPlayerId = memberPlayerId };
			var inviteResp = await leaderClient.PostAsJsonAsync($"/api/alliances/{allianceId}/invite", inviteReq, JsonOptions);
			Assert.Equal(HttpStatusCode.OK, inviteResp.StatusCode);

			// Member checks their invites
			var invitesResp = await memberClient.GetAsync("/api/alliances/my-invites");
			Assert.Equal(HttpStatusCode.OK, invitesResp.StatusCode);
			var invites = await DeserializeAsync<IEnumerable<AllianceInviteViewModel>>(invitesResp);
			Assert.NotNull(invites);
			var inviteList = invites!.ToList();
			Assert.Single(inviteList);

			// Member accepts invite
			var acceptReq = new AcceptInviteRequest { InviteId = inviteList[0].InviteId };
			var acceptResp = await memberClient.PostAsJsonAsync($"/api/alliances/{allianceId}/accept-invite", acceptReq, JsonOptions);
			Assert.Equal(HttpStatusCode.OK, acceptResp.StatusCode);

			// Verify member is in the alliance
			var statusResp = await memberClient.GetAsync("/api/alliances/my-status");
			var statusVm = await DeserializeAsync<MyAllianceStatusViewModel>(statusResp);
			Assert.True(statusVm!.IsMember);
			Assert.Equal(allianceId, statusVm.AllianceId);
		}
	}
}
