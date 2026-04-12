using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.Persistence;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using BrowserGameEngine.StatefulGameServer.GameRegistry;
using BrowserGameEngine.StatefulGameServer.GameTicks.Modules;
using BrowserGameEngine.StatefulGameServer.Events;
using BrowserGameEngine.StatefulGameServer.Notifications;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Linq;
using Xunit;
using GameRegistryNs = BrowserGameEngine.StatefulGameServer.GameRegistry;

namespace BrowserGameEngine.StatefulGameServer.Test {
	public class GameSettingsTest {
		private const string TestGameId = "default";

		private static GameRecordImmutable MakeRecord(
			GameSettings? settings = null,
			GameStatus status = GameStatus.Active
		) {
			return new GameRecordImmutable(
				new GameId(TestGameId),
				"Settings Test Game",
				"sco",
				status,
				DateTime.UtcNow.AddHours(-1),
				DateTime.UtcNow.AddHours(1),
				TimeSpan.FromSeconds(30),
				Settings: settings
			);
		}

		private static TestGame SetupWithSettings(GameSettings? settings) {
			var record = MakeRecord(settings);
			var game = new TestGame(0);
			// Create a GameInstance so the record.Settings propagates to WorldState
			var _ = new GameInstance(record, game.World, game.GameDef);
			return game;
		}

		[Fact]
		public void CreatePlayer_WithCustomSettings_UsesConfiguredStartingResources() {
			var settings = new GameSettings(
				StartingLand: 100,
				StartingMinerals: 10000,
				StartingGas: 7500,
				ProtectionTicks: 240
			);
			var game = SetupWithSettings(settings);

			var playerId = PlayerIdFactory.Create("test-player");
			game.PlayerRepositoryWrite.CreatePlayer(playerId);

			var player = game.PlayerRepository.Get(playerId);
			Assert.Equal(100m, player.State.Resources[Id.ResDef("land")]);
			Assert.Equal(10000m, player.State.Resources[Id.ResDef("minerals")]);
			Assert.Equal(7500m, player.State.Resources[Id.ResDef("gas")]);
			Assert.Equal(240, player.State.ProtectionTicksRemaining);
		}

		[Fact]
		public void CreatePlayer_WithNullSettings_UsesDefaults() {
			var game = SetupWithSettings(settings: null);

			var playerId = PlayerIdFactory.Create("test-player");
			game.PlayerRepositoryWrite.CreatePlayer(playerId);

			var player = game.PlayerRepository.Get(playerId);
			Assert.Equal(50m, player.State.Resources[Id.ResDef("land")]);
			Assert.Equal(5000m, player.State.Resources[Id.ResDef("minerals")]);
			Assert.Equal(3000m, player.State.Resources[Id.ResDef("gas")]);
			Assert.Equal(480, player.State.ProtectionTicksRemaining);
		}

		[Fact]
		public void GameSettings_Default_HasExpectedValues() {
			var defaults = GameSettings.Default;
			Assert.Equal(50, defaults.StartingLand);
			Assert.Equal(5000, defaults.StartingMinerals);
			Assert.Equal(3000, defaults.StartingGas);
			Assert.Equal(480, defaults.ProtectionTicks);
			Assert.Equal(2880, defaults.EndTick);
			Assert.Equal(0, defaults.MaxPlayers);
		}

		[Fact]
		public void GameRecordImmutable_WithNullSettings_IsBackwardCompatible() {
			var record = MakeRecord(settings: null);
			Assert.Null(record.Settings);
		}
	}
}
