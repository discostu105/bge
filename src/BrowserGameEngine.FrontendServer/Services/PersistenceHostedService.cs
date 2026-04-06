using BrowserGameEngine.Persistence;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using BrowserGameEngine.StatefulGameServer.GameRegistry;
using System.Diagnostics;

namespace BrowserGameEngine.FrontendServer {
	public class PersistenceHostedService : IHostedService, IDisposable {
		private int executionCount = 0;
		private int isactive = 0;
		private Timer? timer;
		private readonly ILogger<PersistenceHostedService> logger;
		private readonly PersistenceService persistenceService;
		private readonly GameRegistry gameRegistry;

		public PersistenceHostedService(ILogger<PersistenceHostedService> logger
				, PersistenceService persistenceService
				, GameRegistry gameRegistry
			) {
			this.logger = logger;
			this.persistenceService = persistenceService;
			this.gameRegistry = gameRegistry;
		}

		public Task StartAsync(CancellationToken cancellationToken) {
			timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromSeconds(10));
			return Task.CompletedTask;
		}

		private void DoWork(object? state) {
			if (Interlocked.CompareExchange(ref isactive, 1, 0) != 0) return;
			try {
				StoreAllGameStates().GetAwaiter().GetResult();
			} catch (Exception ex) {
				logger.LogError(ex, "Failed to store game state");
			} finally {
				Interlocked.Exchange(ref isactive, 0);
			}
		}

		private async Task StoreAllGameStates() {
			var sw = Stopwatch.StartNew();
			var count = Interlocked.Increment(ref executionCount);
			foreach (var instance in gameRegistry.GetAllInstances()) {
				var gameId = instance.Record.GameId;
				try {
					await persistenceService.StoreGameState(gameId, instance.WorldState.ToImmutable());
				} catch (Exception ex) {
					logger.LogError(ex, "Failed to store game state for {GameId}", gameId.Id);
				}
			}
			logger.LogInformation("Storing gamestate #{Count} ({GameCount} games) took {Elapsed}", count, gameRegistry.GetAllInstances().Count, sw.Elapsed);
		}

		public async Task StopAsync(CancellationToken cancellationToken) {
			// store state on shutdown
			await StoreAllGameStates();
		}

		public void Dispose() {
			timer?.Dispose();
		}
	}
}
