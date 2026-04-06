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
using System.Collections.Generic;
using Xunit;

namespace BrowserGameEngine.StatefulGameServer.Test {
	public class LobbyTest {
		private static GameRecordImmutable MakeRecord(
			string gameId,
			string? createdByUserId = "creator",
			GameStatus status = GameStatus.Upcoming,
			int maxPlayers = 0
		) {
			var start = DateTime.UtcNow.AddHours(1);
			var end = DateTime.UtcNow.AddDays(7);
			return new GameRecordImmutable(
				new GameId(gameId),
				"Test Game",
				"sco",
				status,
				start,
				end,
				TimeSpan.FromSeconds(30),
				CreatedByUserId: createdByUserId,
				MaxPlayers: maxPlayers
			);
		}

		private static (GamesController controller, GameRegistry.GameRegistry gameRegistry) MakeControllerWithRegistry(
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
			var tournamentRepositoryWrite = new BrowserGameEngine.StatefulGameServer.Repositories.Tournament.TournamentRepositoryWrite(globalState);
			var tournamentEngine = new TournamentEngine(
				globalState, gameRegistry, game.WorldStateFactory, game.GameDef,
				TimeProvider.System, tournamentRepositoryWrite,
				NullLogger<TournamentEngine>.Instance);
			var lifecycleEngine = new GameLifecycleEngine(
				gameRegistry,
				globalState,
				persistenceService,
				globalPersistenceService,
				new NullGameNotificationService(),
				new InMemoryPlayerNotificationService(NullGameEventPublisher.Instance),
				userRepositoryWrite,
				NullGameEventPublisher.Instance,
				TimeProvider.System,
				new BrowserGameEngine.StatefulGameServer.Achievements.MilestoneRepository(globalState, gameRegistry),
				new BrowserGameEngine.StatefulGameServer.Achievements.MilestoneRepositoryWrite(globalState),
				tournamentEngine,
				NullLogger<GameLifecycleEngine>.Instance
			);

			var controller = new GamesController(
				NullLogger<GamesController>.Instance,
				gameRegistry,
				globalState,
				game.WorldStateFactory,
				game.GameDef,
				userCtx,
				TimeProvider.System,
				lifecycleEngine,
				NullGameEventPublisher.Instance,
				game.PlayerRepository,
				game.ScoreRepository,
				new UserRepository(globalState, game.World)
			);

			return (controller, gameRegistry);
		}

		private static GamesController MakeController(GlobalState globalState, string? userId) =>
			MakeControllerWithRegistry(globalState, userId).controller;

		[Fact]
		public void Create_AsNonAdmin_Succeeds() {
			var globalState = new GlobalState();
			var controller = MakeController(globalState, userId: "user1");

			var request = new CreateGameRequest(
				Name: "Player's Game",
				GameDefType: "sco",
				StartTime: DateTime.UtcNow.AddHours(1),
				EndTime: DateTime.UtcNow.AddDays(1),
				TickDuration: "00:00:30"
			);

			var result = controller.Create(request);

			var created = Assert.IsType<CreatedAtActionResult>(result.Result);
			var summary = Assert.IsType<GameSummaryViewModel>(created.Value);
			Assert.Equal("Player's Game", summary.Name);
		}

		[Fact]
		public void Create_WithMaxPlayers_StoresValue() {
			var globalState = new GlobalState();
			var controller = MakeController(globalState, userId: "user1");

			var request = new CreateGameRequest(
				Name: "Limited Game",
				GameDefType: "sco",
				StartTime: DateTime.UtcNow.AddHours(1),
				EndTime: DateTime.UtcNow.AddDays(1),
				TickDuration: "00:00:30",
				MaxPlayers: 8
			);

			var result = controller.Create(request);

			var created = Assert.IsType<CreatedAtActionResult>(result.Result);
			var summary = Assert.IsType<GameSummaryViewModel>(created.Value);
			Assert.Equal(8, summary.MaxPlayers);
		}

		[Fact]
		public void Join_WithRaceSelection_SetsPlayerType() {
			var globalState = new GlobalState();
			var record = MakeRecord("game1");
			globalState.AddGame(record);

			var (controller, gameRegistry) = MakeControllerWithRegistry(globalState, userId: "player1");

			// Register the game instance
			var game = new TestGame(0);
			var instance = new GameInstance(record, game.World, game.GameDef);
			gameRegistry.Register(instance);

			var request = new JoinGameRequest("Commander", PlayerType: "type2");
			var result = controller.Join("game1", request);

			Assert.IsType<OkObjectResult>(result);

			// Verify the player was created with the correct type
			Assert.Equal(1, instance.PlayerCount);
		}

		[Fact]
		public void Join_WithInvalidRace_Returns400() {
			var globalState = new GlobalState();
			var record = MakeRecord("game1");
			globalState.AddGame(record);

			var (controller, gameRegistry) = MakeControllerWithRegistry(globalState, userId: "player1");

			var game = new TestGame(0);
			var instance = new GameInstance(record, game.World, game.GameDef);
			gameRegistry.Register(instance);

			var request = new JoinGameRequest("Commander", PlayerType: "invalid_race");
			var result = controller.Join("game1", request);

			Assert.IsType<BadRequestObjectResult>(result);
		}

		[Fact]
		public void Join_WhenGameFull_Returns400() {
			var globalState = new GlobalState();
			var record = MakeRecord("game1", maxPlayers: 1);
			globalState.AddGame(record);

			var (controller, gameRegistry) = MakeControllerWithRegistry(globalState, userId: "player2");

			// Create a game instance with 1 existing player (at max capacity)
			var game = new TestGame(1);
			var instance = new GameInstance(record, game.World, game.GameDef);
			gameRegistry.Register(instance);

			var request = new JoinGameRequest("Commander2");
			var result = controller.Join("game1", request);

			Assert.IsType<BadRequestObjectResult>(result);
		}

		[Fact]
		public void GetLobby_ReturnsPlayerList() {
			var globalState = new GlobalState();
			var record = MakeRecord("game1");
			globalState.AddGame(record);

			var (controller, gameRegistry) = MakeControllerWithRegistry(globalState, userId: "viewer");

			var game = new TestGame(2);
			var instance = new GameInstance(record, game.World, game.GameDef);
			gameRegistry.Register(instance);

			var result = controller.GetLobby("game1");

			var okResult = Assert.IsType<OkObjectResult>(result.Result);
			var lobby = Assert.IsType<GameLobbyViewModel>(okResult.Value);
			Assert.Equal("game1", lobby.GameId);
			Assert.Equal(2, lobby.Players.Count);
		}

		[Fact]
		public void GetLobby_NonexistentGame_Returns404() {
			var globalState = new GlobalState();
			var controller = MakeController(globalState, userId: "viewer");

			var result = controller.GetLobby("nonexistent");

			Assert.IsType<NotFoundResult>(result.Result);
		}

		[Fact]
		public void GetLobby_ShowsCanJoinFalse_WhenGameFull() {
			var globalState = new GlobalState();
			var record = MakeRecord("game1", maxPlayers: 2);
			globalState.AddGame(record);

			var (controller, gameRegistry) = MakeControllerWithRegistry(globalState, userId: "viewer");

			var game = new TestGame(2);
			var instance = new GameInstance(record, game.World, game.GameDef);
			gameRegistry.Register(instance);

			var result = controller.GetLobby("game1");

			var okResult = Assert.IsType<OkObjectResult>(result.Result);
			var lobby = Assert.IsType<GameLobbyViewModel>(okResult.Value);
			Assert.False(lobby.CanJoin);
		}

		[Fact]
		public void GetRaces_ReturnsAvailableRaces() {
			var globalState = new GlobalState();
			var controller = MakeController(globalState, userId: "user1");

			var result = controller.GetRaces();

			var okResult = Assert.IsType<OkObjectResult>(result.Result);
			var raceList = Assert.IsType<RaceListViewModel>(okResult.Value);
			Assert.Equal(2, raceList.Races.Count);
			Assert.Contains(raceList.Races, r => r.Id == "type1");
			Assert.Contains(raceList.Races, r => r.Id == "type2");
		}

		[Fact]
		public void Join_DefaultsToTerran_WhenNoRaceSpecified() {
			var globalState = new GlobalState();
			var record = MakeRecord("game1");
			globalState.AddGame(record);

			var (controller, gameRegistry) = MakeControllerWithRegistry(globalState, userId: "player1");

			var game = new TestGame(0);
			var instance = new GameInstance(record, game.World, game.GameDef);
			gameRegistry.Register(instance);

			var request = new JoinGameRequest("Commander");
			var result = controller.Join("game1", request);

			Assert.IsType<OkObjectResult>(result);
			Assert.Equal(1, instance.PlayerCount);
		}
	}
}
