using BrowserGameEngine.GameModel;
using BrowserGameEngine.Persistence;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace BrowserGameEngine.FrontendServer.Services {
	public class MigrationService {
		private readonly IBlobStorage storage;
		private readonly PersistenceService persistenceService;
		private readonly GlobalPersistenceService globalPersistenceService;
		private readonly ILogger<MigrationService> logger;

		public MigrationService(IBlobStorage storage, PersistenceService persistenceService, GlobalPersistenceService globalPersistenceService, ILogger<MigrationService> logger) {
			this.storage = storage;
			this.persistenceService = persistenceService;
			this.globalPersistenceService = globalPersistenceService;
			this.logger = logger;
		}

		public async Task MigrateIfNeeded() {
			if (globalPersistenceService.GlobalStateExists()) {
				logger.LogInformation("Migration: already migrated, skipping.");
				return;
			}
			if (!storage.Exists("latest.json")) {
				logger.LogInformation("Migration: no legacy state found, fresh install.");
				return;
			}

			logger.LogInformation("Migration: migrating legacy latest.json to multi-game layout.");

			var legacyBytes = await storage.Load("latest.json");
			var legacyJson = JsonDocument.Parse(legacyBytes);

			var users = new Dictionary<string, UserImmutable>();
			if (legacyJson.RootElement.TryGetProperty("Users", out var usersElement) &&
				usersElement.ValueKind != JsonValueKind.Null) {
				var usersDict = JsonSerializer.Deserialize<Dictionary<string, UserImmutable>>(usersElement.GetRawText());
				if (usersDict != null) users = usersDict;
			}

			var worldState = persistenceService.DeserializeLegacy(legacyBytes);
			var migratedGameId = new GameId("league-round-1");
			var migratedWorldState = worldState with { GameId = migratedGameId };

			await persistenceService.StoreGameState(migratedGameId, migratedWorldState);
			logger.LogInformation("Migration: wrote games/league-round-1/state.json");

			var gameRecord = new GameRecordImmutable(
				GameId: migratedGameId,
				Name: "BGE League — Round 1",
				GameDefType: "sco",
				Status: GameStatus.Active,
				StartTime: DateTime.UtcNow - TimeSpan.FromDays(1),
				EndTime: DateTime.UtcNow + TimeSpan.FromDays(3650),
				TickDuration: TimeSpan.FromSeconds(10)
			);

			var globalState = new GlobalStateImmutable(
				Users: users,
				Games: new List<GameRecordImmutable> { gameRecord },
				Achievements: new List<PlayerAchievementImmutable>()
			);

			await globalPersistenceService.StoreGlobalState(globalState);
			logger.LogInformation("Migration: wrote global/state.json");

			try {
				var legacyCopy = await storage.Load("latest.json");
				await storage.Store("latest.json.migrated", legacyCopy);
				await storage.Delete("latest.json");
				logger.LogInformation("Migration: renamed latest.json to latest.json.migrated");
			} catch (Exception ex) {
				logger.LogWarning(ex, "Migration: could not rename latest.json — continuing anyway");
			}

			logger.LogInformation("Migration: complete.");
		}
	}
}
