using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer;
using BrowserGameEngine.StatefulGameServer.Bots;
using Microsoft.Extensions.Options;

namespace BrowserGameEngine.FrontendServer {
	/// <summary>
	/// Creates bot players at startup based on configuration (Bge:Bots:BotCount / Bge:Bots:BotRace).
	/// Already-existing bots (from persisted state) are left untouched.
	/// </summary>
	public class BotSpawnerHostedService : IHostedService {
		private readonly ILogger<BotSpawnerHostedService> logger;
		private readonly PlayerRepository playerRepository;
		private readonly PlayerRepositoryWrite playerRepositoryWrite;
		private readonly BotOptions botOptions;

		public BotSpawnerHostedService(
			ILogger<BotSpawnerHostedService> logger,
			PlayerRepository playerRepository,
			PlayerRepositoryWrite playerRepositoryWrite,
			IOptions<BotOptions> botOptions
		) {
			this.logger = logger;
			this.playerRepository = playerRepository;
			this.playerRepositoryWrite = playerRepositoryWrite;
			this.botOptions = botOptions.Value;
		}

		public Task StartAsync(CancellationToken cancellationToken) {
			SpawnMissingBots();
			return Task.CompletedTask;
		}

		public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

		private void SpawnMissingBots() {
			int count = botOptions.BotCount;
			if (count <= 0) return;

			var race = Id.PlayerType(string.IsNullOrWhiteSpace(botOptions.BotRace) ? "terran" : botOptions.BotRace);
			var hqAsset = GetHqForRace(race);

			for (int i = 0; i < count; i++) {
				var playerId = PlayerIdFactory.Create($"bot_{i}");
				if (!playerRepository.Exists(playerId)) {
					playerRepositoryWrite.CreateBotPlayer(playerId, $"[BOT] {race.Id} #{i}", race, hqAsset);
					logger.LogInformation("Spawned bot player {PlayerId} ({Race})", playerId, race.Id);
				}
			}
		}

		private static AssetDefId GetHqForRace(PlayerTypeDefId playerType) {
			return playerType.Id switch {
				"terran" => Id.AssetDef("commandcenter"),
				"zerg" => Id.AssetDef("hive"),
				"protoss" => Id.AssetDef("nexus"),
				_ => Id.AssetDef("commandcenter")
			};
		}
	}
}
