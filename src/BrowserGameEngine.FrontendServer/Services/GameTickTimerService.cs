using BrowserGameEngine.StatefulGameServer.GameTicks;
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
		private readonly GameTickEngine gameTickEngine;
		private Timer? timer;

		public GameTickTimerService(ILogger<GameTickTimerService> logger, GameTickEngine gameTickEngine) {
			this.logger = logger;
			this.gameTickEngine = gameTickEngine;
		}

		public Task StartAsync(CancellationToken stoppingToken) {
			logger.LogInformation("Timed Hosted Service running.");
			timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
			return Task.CompletedTask;
		}

		private void DoWork(object? state) {
			if (isactive == 1) return; // avoid multipe timers at once. if old timer task is still running, just skip this time.
			Interlocked.Increment(ref isactive);
			try {
				var count = Interlocked.Increment(ref executionCount);
				logger.LogInformation("GameTickTimer #{Count}", count);
				gameTickEngine.CheckAllTicks();
			} finally {
				Interlocked.Decrement(ref isactive);
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
