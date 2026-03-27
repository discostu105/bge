using BrowserGameEngine.Persistence;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using BrowserGameEngine.GameModel;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BrowserGameEngine.FrontendServer {
	public class PersistenceHostedService : IHostedService, IDisposable {
		private int executionCount = 0;
		private int isactive = 0;
		private Timer? timer;
		private readonly ILogger<PersistenceHostedService> logger;
		private readonly PersistenceService persistenceService;
		private readonly WorldState worldState;

		public PersistenceHostedService(ILogger<PersistenceHostedService> logger
				, PersistenceService persistenceService
				, WorldState worldState
			) {
			this.logger = logger;
			this.persistenceService = persistenceService;
			this.worldState = worldState;
		}

		public Task StartAsync(CancellationToken cancellationToken) {
			timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromSeconds(10));
			return Task.CompletedTask;
		}

		private void DoWork(object? state) {
			if (Interlocked.CompareExchange(ref isactive, 1, 0) != 0) return;
			try {
				StoreGameState().GetAwaiter().GetResult();
			} catch (Exception ex) {
				logger.LogError(ex, "Failed to store game state");
			} finally {
				Interlocked.Exchange(ref isactive, 0);
			}
		}

		private async Task StoreGameState() {
			var sw = Stopwatch.StartNew();
			var count = Interlocked.Increment(ref executionCount);
			await persistenceService.StoreWorldSate(worldState.ToImmutable());
			logger.LogInformation("Storing gamestate #{Count} took {Elapsed}", count, sw.Elapsed);
		}

		public async Task StopAsync(CancellationToken cancellationToken) {
			// store state on shutdown
			await StoreGameState();
		}

		public void Dispose() {
			timer?.Dispose();
		}
	}
}
