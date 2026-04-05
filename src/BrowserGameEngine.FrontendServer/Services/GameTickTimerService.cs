using BrowserGameEngine.StatefulGameServer.GameRegistry;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
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
				var sw = System.Diagnostics.Stopwatch.StartNew();
				foreach (var instance in gameRegistry.GetAllInstances()) {
					instance.TickEngine?.CheckAllTicks();
				}
				sw.Stop();
				logger.LogInformation("GameTick #{Count} completed in {ElapsedMs}ms", count, sw.ElapsedMilliseconds);
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
