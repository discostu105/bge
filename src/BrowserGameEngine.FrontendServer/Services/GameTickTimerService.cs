using BrowserGameEngine.StatefulGameServer;
using BrowserGameEngine.StatefulGameServer.GameRegistry;
using BrowserGameEngine.StatefulGameServer.GameTicks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace BrowserGameEngine.FrontendServer {
	/// <summary>
	/// Drives <see cref="GameTickEngine"/> for every registered game.
	///
	/// The tick engine and its modules are singletons that resolve the current
	/// <see cref="BrowserGameEngine.StatefulGameServer.GameModelInternal.WorldState"/>
	/// via <see cref="IWorldStateAccessor"/>. In an HTTP request the accessor
	/// reads the per-game world from the X-Game-Id header; outside an HTTP
	/// request (i.e. here) it falls back to the default world. So before
	/// ticking a given instance we explicitly push that instance's WorldState
	/// as the ambient override — otherwise every iteration would tick the same
	/// default world and per-game progress would freeze (BGE: build queues
	/// stuck at "ready in Nt" forever on non-default games).
	/// </summary>
	public class GameTickTimerService : IHostedService, IDisposable {
		private int executionCount = 0;
		private int isactive = 0;
		private readonly ILogger<GameTickTimerService> logger;
		private readonly GameRegistry gameRegistry;
		private readonly GameTickEngine tickEngine;
		private readonly IWorldStateAccessor worldStateAccessor;
		private Timer? timer;

		public GameTickTimerService(
			ILogger<GameTickTimerService> logger,
			GameRegistry gameRegistry,
			GameTickEngine tickEngine,
			IWorldStateAccessor worldStateAccessor) {
			this.logger = logger;
			this.gameRegistry = gameRegistry;
			this.tickEngine = tickEngine;
			this.worldStateAccessor = worldStateAccessor;
		}

		public Task StartAsync(CancellationToken stoppingToken) {
			logger.LogInformation("Timed Hosted Service running.");
			timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromSeconds(10));
			return Task.CompletedTask;
		}

		private void DoWork(object? state) {
			if (Interlocked.CompareExchange(ref isactive, 1, 0) != 0) return;
			try {
				var count = Interlocked.Increment(ref executionCount);
				var totalSw = Stopwatch.StartNew();
				foreach (var instance in gameRegistry.GetAllInstances()) {
					if (instance.IsPaused) continue;
					var sw = Stopwatch.StartNew();
					try {
						using var scope = worldStateAccessor.PushAmbient(instance.WorldState);
						tickEngine.CheckAllTicks();
					} catch (Exception ex) {
						logger.LogError(ex, "GameTickFailure GameId={GameId}", instance.Record.GameId.Id);
					} finally {
						sw.Stop();
						logger.LogInformation(
							"GameTickCompleted GameId={GameId} TickDurationMs={TickDurationMs} PlayerCount={PlayerCount}",
							instance.Record.GameId.Id, sw.ElapsedMilliseconds, instance.PlayerCount);
					}
				}
				totalSw.Stop();
				logger.LogInformation("GameTick #{Count} completed in {ElapsedMs}ms", count, totalSw.ElapsedMilliseconds);
			} finally {
				Interlocked.Exchange(ref isactive, 0);
			}
		}

		public Task StopAsync(CancellationToken stoppingToken) {
			logger.LogInformation("Timed Hosted Service is stopping.");
			timer?.Change(Timeout.Infinite, 0);
			return Task.CompletedTask;
		}

		public void Dispose() {
			timer?.Dispose();
		}
	}
}
