using BrowserGameEngine.FrontendServer;
using BrowserGameEngine.FrontendServer.Controllers;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.Persistence;
using BrowserGameEngine.Shared;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using BrowserGameEngine.StatefulGameServer.GameRegistry;
using BrowserGameEngine.StatefulGameServer.Events;
using BrowserGameEngine.StatefulGameServer.Notifications;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using Xunit;

namespace BrowserGameEngine.StatefulGameServer.Test {
	/// <summary>
	/// Unit tests for GamesController.Update (PATCH /api/games/{gameId}).
	/// Covers BGE-266: ownership enforcement, EndTime validation, and successful mutation.
	/// </summary>
	public class GamesControllerTest {
		private static GameRecordImmutable MakeRecord(
			string gameId,
			string? createdByUserId,
			GameStatus status = GameStatus.Upcoming,
			string? webhookUrl = null
		) {
			var start = DateTime.UtcNow.AddHours(-1);
			var end = status == GameStatus.Finished
				? DateTime.UtcNow.AddMinutes(-10)
				: DateTime.UtcNow.AddDays(1);
			return new GameRecordImmutable(
				new GameId(gameId),
				"Test Game",
				"sco",
				status,
				start,
				end,
				TimeSpan.FromSeconds(10),
				DiscordWebhookUrl: webhookUrl,
				CreatedByUserId: createdByUserId
			);
		}

		private static GamesController MakeController(
			GlobalState globalState,
			string? userId
		) {
			var game = new TestGame();
			var gameRegistry = new GameRegistry.GameRegistry(globalState);
			var userCtx = new CurrentUserContext();
			if (userId != null) {
				userCtx.UserId = userId;
				userCtx.Activate(PlayerIdFactory.Create(userId));
			}

			var storage = new InMemoryBlobStorage();
			var persistenceService = new PersistenceService(storage, new GameStateJsonSerializer());
			var globalPersistenceService = new GlobalPersistenceService(storage, new GlobalStateJsonSerializer());
			var userRepositoryWrite = new UserRepositoryWrite(globalState, game.World, TimeProvider.System);
			var lifecycleEngine = new GameRegistry.GameLifecycleEngine(
				gameRegistry,
				globalState,
				persistenceService,
				globalPersistenceService,
				new NullGameNotificationService(),
				new InMemoryPlayerNotificationService(NullGameEventPublisher.Instance),
				userRepositoryWrite,
				NullGameEventPublisher.Instance,
				TimeProvider.System,
				NullLogger<GameRegistry.GameLifecycleEngine>.Instance
			);

			return new GamesController(
				NullLogger<GamesController>.Instance,
				gameRegistry,
				globalState,
				game.WorldStateFactory,
				game.GameDef,
				userCtx,
				TimeProvider.System,
				lifecycleEngine
			);
		}

		[Fact]
		public void Update_AsNonCreator_Returns403() {
			var globalState = new GlobalState();
			var record = MakeRecord("game1", createdByUserId: "alice");
			globalState.AddGame(record);

			var controller = MakeController(globalState, userId: "bob");
			var request = new UpdateGameRequest(
				Name: "New Name",
				EndTime: DateTime.UtcNow.AddDays(2)
			);

			var result = controller.Update("game1", request);

			var objectResult = Assert.IsType<ObjectResult>(result.Result);
			Assert.Equal(403, objectResult.StatusCode);
		}

		[Fact]
		public void Update_EndTimeBeforeStartTime_Returns400() {
			var globalState = new GlobalState();
			var record = MakeRecord("game2", createdByUserId: "alice");
			globalState.AddGame(record);

			var controller = MakeController(globalState, userId: "alice");
			var request = new UpdateGameRequest(
				Name: "New Name",
				EndTime: record.StartTime.AddMinutes(-1)
			);

			var result = controller.Update("game2", request);

			var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
			Assert.NotNull(badRequest);
		}

		[Fact]
		public void Update_ShortenEndTimeOnActiveGame_Returns400() {
			var globalState = new GlobalState();
			var record = MakeRecord("game3", createdByUserId: "alice", status: GameStatus.Active);
			globalState.AddGame(record);

			var controller = MakeController(globalState, userId: "alice");
			var request = new UpdateGameRequest(
				Name: "New Name",
				EndTime: record.EndTime.AddHours(-1)
			);

			var result = controller.Update("game3", request);

			var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
			Assert.NotNull(badRequest);
		}

		[Fact]
		public void Update_AsCreator_MutatesGame() {
			var globalState = new GlobalState();
			var record = MakeRecord("game4", createdByUserId: "alice", webhookUrl: "https://old.hook");
			globalState.AddGame(record);

			var controller = MakeController(globalState, userId: "alice");
			var newEnd = record.EndTime.AddDays(1);
			var request = new UpdateGameRequest(
				Name: "Updated Name",
				EndTime: newEnd,
				DiscordWebhookUrl: "https://new.hook"
			);

			var result = controller.Update("game4", request);

			var okResult = Assert.IsType<OkObjectResult>(result.Result);
			var detail = Assert.IsType<GameDetailViewModel>(okResult.Value);
			Assert.Equal("Updated Name", detail.Name);
			Assert.Equal(newEnd.ToUniversalTime(), detail.EndTime);

			// Verify GlobalState was mutated
			var updated = globalState.GetGames()[0];
			Assert.Equal("Updated Name", updated.Name);
		}

		[Fact]
		public void Update_NullCreatorUserId_DeniesEdit() {
			// Games with null CreatedByUserId cannot be edited by anyone (security fix)
			var globalState = new GlobalState();
			var record = MakeRecord("game5", createdByUserId: null);
			globalState.AddGame(record);

			var controller = MakeController(globalState, userId: "anyone");
			var request = new UpdateGameRequest(
				Name: "Edited by Anyone",
				EndTime: record.EndTime.AddDays(1)
			);

			var result = controller.Update("game5", request);

			var statusResult = Assert.IsType<ObjectResult>(result.Result);
			Assert.Equal(403, statusResult.StatusCode);
		}
	}
}
