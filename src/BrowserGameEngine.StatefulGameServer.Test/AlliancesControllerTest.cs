using BrowserGameEngine.FrontendServer;
using BrowserGameEngine.FrontendServer.Controllers;
using BrowserGameEngine.GameModel;
using System;
using BrowserGameEngine.Shared;
using BrowserGameEngine.StatefulGameServer;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using BrowserGameEngine.StatefulGameServer.Notifications;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace BrowserGameEngine.StatefulGameServer.Test {
	public class AlliancesControllerTest {
		private static AlliancesController MakeController(TestGame game, CurrentUserContext userCtx) {
			return new AlliancesController(
				userCtx,
				game.AllianceRepository,
				game.AllianceRepositoryWrite,
				new AllianceChatRepository(game.Accessor),
				new AllianceChatRepositoryWrite(game.Accessor, TimeProvider.System),
				game.PlayerRepository,
				NullNotificationService.Instance,
				game.AllianceInviteRepository,
				game.AllianceInviteRepositoryWrite,
				game.AllianceWarRepository,
				game.AllianceWarRepositoryWrite
			);
		}

		private static CurrentUserContext AuthenticatedContext(PlayerId playerId) {
			var ctx = new CurrentUserContext();
			ctx.UserId = playerId.Id;
			ctx.Activate(playerId);
			return ctx;
		}

		[Fact]
		public void GetAll_EmptyState_ReturnsEmptyList() {
			var game = new TestGame(playerCount: 1);
			var player1 = PlayerIdFactory.Create("player0");
			var ctx = AuthenticatedContext(player1);
			var controller = MakeController(game, ctx);

			var result = controller.GetAll();

			var ok = Assert.IsType<OkObjectResult>(result.Result);
			var alliances = Assert.IsAssignableFrom<IEnumerable<AllianceViewModel>>(ok.Value);
			Assert.Empty(alliances);
		}

		[Fact]
		public void Create_WhenUnauthorized_ReturnsUnauthorized() {
			var game = new TestGame(playerCount: 1);
			var ctx = new CurrentUserContext();
			var controller = MakeController(game, ctx);

			var result = controller.Create(new CreateAllianceRequest { AllianceName = "TestAlliance", Password = "" });

			Assert.IsType<UnauthorizedResult>(result.Result);
		}

		[Fact]
		public void Create_HappyPath_AllianceAppearsInGetAll() {
			var game = new TestGame(playerCount: 1);
			var player1 = PlayerIdFactory.Create("player0");
			var ctx = AuthenticatedContext(player1);
			var controller = MakeController(game, ctx);

			var result = controller.Create(new CreateAllianceRequest { AllianceName = "MyAlliance", Password = "" });

			Assert.IsType<OkObjectResult>(result.Result);
			var getAllResult = controller.GetAll();
			var getAllOk = Assert.IsType<OkObjectResult>(getAllResult.Result);
			var alliances = Assert.IsAssignableFrom<IEnumerable<AllianceViewModel>>(getAllOk.Value).ToList();
			Assert.Single(alliances);
			Assert.Equal("MyAlliance", alliances[0].Name);
		}

		[Fact]
		public void Create_DuplicateName_Returns409() {
			var game = new TestGame(playerCount: 2);
			var player1 = PlayerIdFactory.Create("player0");
			var player2 = PlayerIdFactory.Create("player1");
			var ctrl1 = MakeController(game, AuthenticatedContext(player1));
			var ctrl2 = MakeController(game, AuthenticatedContext(player2));

			ctrl1.Create(new CreateAllianceRequest { AllianceName = "Conflict", Password = "" });

			var result = ctrl2.Create(new CreateAllianceRequest { AllianceName = "Conflict", Password = "" });

			Assert.IsType<ConflictObjectResult>(result.Result);
		}

		[Fact]
		public void Create_AlreadyInAlliance_Returns400() {
			var game = new TestGame(playerCount: 1);
			var player1 = PlayerIdFactory.Create("player0");
			var ctx = AuthenticatedContext(player1);
			var controller = MakeController(game, ctx);

			controller.Create(new CreateAllianceRequest { AllianceName = "First", Password = "" });

			var result = controller.Create(new CreateAllianceRequest { AllianceName = "Second", Password = "" });

			Assert.IsType<BadRequestObjectResult>(result.Result);
		}

		[Fact]
		public void GetById_NotFound_Returns404() {
			var game = new TestGame(playerCount: 1);
			var player1 = PlayerIdFactory.Create("player0");
			var ctx = AuthenticatedContext(player1);
			var controller = MakeController(game, ctx);

			var result = controller.GetById("00000000-0000-0000-0000-000000000001");

			Assert.IsType<NotFoundResult>(result.Result);
		}

		[Fact]
		public void GetById_HappyPath_ReturnsAllianceDetail() {
			var game = new TestGame(playerCount: 1);
			var player1 = PlayerIdFactory.Create("player0");
			var ctx = AuthenticatedContext(player1);
			var controller = MakeController(game, ctx);

			controller.Create(new CreateAllianceRequest { AllianceName = "DetailTest", Password = "" });
			var allianceId = game.AllianceRepository.GetAll().First().AllianceId.ToString();

			var result = controller.GetById(allianceId);

			var ok = Assert.IsType<OkObjectResult>(result.Result);
			var detail = Assert.IsType<AllianceDetailViewModel>(ok.Value);
			Assert.Equal("DetailTest", detail.Name);
			Assert.Single(detail.Members);
		}

		[Fact]
		public void MyStatus_WhenUnauthorized_ReturnsUnauthorized() {
			var game = new TestGame(playerCount: 1);
			var ctx = new CurrentUserContext();
			var controller = MakeController(game, ctx);

			var result = controller.MyStatus();

			Assert.IsType<UnauthorizedResult>(result.Result);
		}

		[Fact]
		public void MyStatus_NotInAlliance_ReturnsIsMemberFalse() {
			var game = new TestGame(playerCount: 1);
			var player1 = PlayerIdFactory.Create("player0");
			var ctx = AuthenticatedContext(player1);
			var controller = MakeController(game, ctx);

			var result = controller.MyStatus();

			var ok = Assert.IsType<OkObjectResult>(result.Result);
			var status = Assert.IsType<MyAllianceStatusViewModel>(ok.Value);
			Assert.False(status.IsMember);
		}

		[Fact]
		public void MyStatus_AfterCreate_ReturnsIsMemberAndLeader() {
			var game = new TestGame(playerCount: 1);
			var player1 = PlayerIdFactory.Create("player0");
			var ctx = AuthenticatedContext(player1);
			var controller = MakeController(game, ctx);

			controller.Create(new CreateAllianceRequest { AllianceName = "StatusTest", Password = "" });

			var result = controller.MyStatus();

			var ok = Assert.IsType<OkObjectResult>(result.Result);
			var status = Assert.IsType<MyAllianceStatusViewModel>(ok.Value);
			Assert.True(status.IsMember);
			Assert.True(status.IsLeader);
			Assert.Equal("StatusTest", status.AllianceName);
		}

		[Fact]
		public void InvitePlayer_WhenNotLeader_Returns403() {
			var game = new TestGame(playerCount: 3);
			var player1 = PlayerIdFactory.Create("player0");
			var player2 = PlayerIdFactory.Create("player1");
			var player3 = PlayerIdFactory.Create("player2");

			var ctrl1 = MakeController(game, AuthenticatedContext(player1));
			ctrl1.Create(new CreateAllianceRequest { AllianceName = "InviteTest", Password = "" });
			var allianceId = game.AllianceRepository.GetAll().First().AllianceId.ToString();

			var ctrl2 = MakeController(game, AuthenticatedContext(player2));
			ctrl2.Join(allianceId, new JoinAllianceRequest { Password = "" });
			ctrl1.AcceptMember(allianceId, player2.Id);

			// Non-leader tries to invite
			var result = ctrl2.InvitePlayer(allianceId, new InvitePlayerRequest { TargetPlayerId = player3.Id });

			var statusResult = Assert.IsType<ObjectResult>(result);
			Assert.Equal(403, statusResult.StatusCode);
		}

		[Fact]
		public void InvitePlayer_TargetNotFound_Returns404() {
			var game = new TestGame(playerCount: 1);
			var player1 = PlayerIdFactory.Create("player0");
			var ctx = AuthenticatedContext(player1);
			var controller = MakeController(game, ctx);

			controller.Create(new CreateAllianceRequest { AllianceName = "InviteTest2", Password = "" });
			var allianceId = game.AllianceRepository.GetAll().First().AllianceId.ToString();

			var result = controller.InvitePlayer(allianceId, new InvitePlayerRequest { TargetPlayerId = "nonexistent-player" });

			Assert.IsType<NotFoundResult>(result);
		}

		[Fact]
		public void InvitePlayer_HappyPath_InviteAppearsInRecipientInbox() {
			var game = new TestGame(playerCount: 2);
			var player1 = PlayerIdFactory.Create("player0");
			var player2 = PlayerIdFactory.Create("player1");

			var ctrl1 = MakeController(game, AuthenticatedContext(player1));
			ctrl1.Create(new CreateAllianceRequest { AllianceName = "InviteHappy", Password = "" });
			var allianceId = game.AllianceRepository.GetAll().First().AllianceId.ToString();

			ctrl1.InvitePlayer(allianceId, new InvitePlayerRequest { TargetPlayerId = player2.Id });

			var ctrl2 = MakeController(game, AuthenticatedContext(player2));
			var result = ctrl2.GetMyInvites();
			var ok = Assert.IsType<OkObjectResult>(result.Result);
			var invites = Assert.IsAssignableFrom<IEnumerable<AllianceInviteViewModel>>(ok.Value);
			Assert.Single(invites);
		}

		[Fact]
		public void Leave_WhenUnauthorized_ReturnsUnauthorized() {
			var game = new TestGame(playerCount: 1);
			var ctx = new CurrentUserContext();
			var controller = MakeController(game, ctx);

			var result = controller.Leave();

			Assert.IsType<UnauthorizedResult>(result);
		}

		[Fact]
		public void Leave_WhenNotInAlliance_Returns400() {
			var game = new TestGame(playerCount: 1);
			var player1 = PlayerIdFactory.Create("player0");
			var ctx = AuthenticatedContext(player1);
			var controller = MakeController(game, ctx);

			var result = controller.Leave();

			Assert.IsType<BadRequestObjectResult>(result);
		}

		[Fact]
		public void DeclareWar_HappyPath_WarAppearsInGetWars() {
			var game = new TestGame(playerCount: 2);
			var player1 = PlayerIdFactory.Create("player0");
			var player2 = PlayerIdFactory.Create("player1");

			var ctrl1 = MakeController(game, AuthenticatedContext(player1));
			ctrl1.Create(new CreateAllianceRequest { AllianceName = "WarA", Password = "" });
			var allianceAId = game.AllianceRepository.GetAll().First(a => a.Name == "WarA").AllianceId.ToString();

			var ctrl2 = MakeController(game, AuthenticatedContext(player2));
			ctrl2.Create(new CreateAllianceRequest { AllianceName = "WarB", Password = "" });
			var allianceBId = game.AllianceRepository.GetAll().First(a => a.Name == "WarB").AllianceId.ToString();

			ctrl1.DeclareWar(allianceAId, new DeclareWarRequest { TargetAllianceId = allianceBId });

			var result = ctrl1.GetWars(allianceAId);
			var ok = Assert.IsType<OkObjectResult>(result.Result);
			var wars = Assert.IsAssignableFrom<IEnumerable<AllianceWarViewModel>>(ok.Value);
			Assert.Single(wars);
		}

		[Fact]
		public void DeclareWar_WhenNotLeader_Returns403() {
			var game = new TestGame(playerCount: 3);
			var player1 = PlayerIdFactory.Create("player0");
			var player2 = PlayerIdFactory.Create("player1");
			var player3 = PlayerIdFactory.Create("player2");

			var ctrl1 = MakeController(game, AuthenticatedContext(player1));
			ctrl1.Create(new CreateAllianceRequest { AllianceName = "AllianceA", Password = "" });
			var allianceAId = game.AllianceRepository.GetAll().First(a => a.Name == "AllianceA").AllianceId.ToString();

			var ctrl2 = MakeController(game, AuthenticatedContext(player2));
			ctrl2.Join(allianceAId, new JoinAllianceRequest { Password = "" });
			ctrl1.AcceptMember(allianceAId, player2.Id);

			var ctrl3 = MakeController(game, AuthenticatedContext(player3));
			ctrl3.Create(new CreateAllianceRequest { AllianceName = "AllianceB", Password = "" });
			var allianceBId = game.AllianceRepository.GetAll().First(a => a.Name == "AllianceB").AllianceId.ToString();

			// player2 (non-leader) tries to declare war
			var result = ctrl2.DeclareWar(allianceAId, new DeclareWarRequest { TargetAllianceId = allianceBId });

			var statusResult = Assert.IsType<ObjectResult>(result);
			Assert.Equal(403, statusResult.StatusCode);
		}

		[Fact]
		public void GetMyInvites_WhenUnauthorized_ReturnsUnauthorized() {
			var game = new TestGame(playerCount: 1);
			var ctx = new CurrentUserContext();
			var controller = MakeController(game, ctx);

			var result = controller.GetMyInvites();

			Assert.IsType<UnauthorizedResult>(result.Result);
		}

		[Fact]
		public void GetMyInvites_NoInvites_ReturnsEmptyList() {
			var game = new TestGame(playerCount: 1);
			var player1 = PlayerIdFactory.Create("player0");
			var ctx = AuthenticatedContext(player1);
			var controller = MakeController(game, ctx);

			var result = controller.GetMyInvites();

			var ok = Assert.IsType<OkObjectResult>(result.Result);
			var invites = Assert.IsAssignableFrom<IEnumerable<AllianceInviteViewModel>>(ok.Value);
			Assert.Empty(invites);
		}
	}
}
