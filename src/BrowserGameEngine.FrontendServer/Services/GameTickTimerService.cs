using BrowserGameEngine.StatefulGameServer.GameRegistry;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace BrowserGameEngine.FrontendServer {
	public class GameTickTimerService : IHostedService, IDisposable {
		private int executionCount = 0;
		private int isactive = 0;
		private readonly ILogger<GameTickTimerService> logger;
		private readonly GameRegistry gameRegistry;
		private Timer? timer;

		public GameTickTimerService(ILogger<GameTickTimerService> logger, GameRegistry gameRegistry) {
			this.logger = logger;
			this.gameRegistry = gameRegistry;
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
					var sw = Stopwatch.StartNew();
					try {
						instance.TickEngine?.CheckAllTicks();
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
