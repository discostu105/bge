using BrowserGameEngine.StatefulGameServer.GameRegistry;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BrowserGameEngine.FrontendServer {
	public class GameLifecycleService : IHostedService, IDisposable {
		private readonly ILogger<GameLifecycleService> logger;
		private readonly GameLifecycleEngine lifecycleEngine;
		private int isActive = 0;
		private Timer? timer;

		public GameLifecycleService(ILogger<GameLifecycleService> logger, GameLifecycleEngine lifecycleEngine) {
			this.logger = logger;
			this.lifecycleEngine = lifecycleEngine;
		}

		public Task StartAsync(CancellationToken stoppingToken) {
			timer = new Timer(DoWork, null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(60));
			return Task.CompletedTask;
		}

		private void DoWork(object? state) {
			if (Interlocked.CompareExchange(ref isActive, 1, 0) != 0) return;
			try {
				lifecycleEngine.ProcessLifecycleAsync().GetAwaiter().GetResult();
			} catch (Exception ex) {
				logger.LogError(ex, "GameLifecycleService: error processing game lifecycle");
			} finally {
				Interlocked.Exchange(ref isActive, 0);
			}
		}

		public Task StopAsync(CancellationToken stoppingToken) {
			timer?.Change(Timeout.Infinite, 0);
			return Task.CompletedTask;
		}

		public void Dispose() => timer?.Dispose();
	}
}
