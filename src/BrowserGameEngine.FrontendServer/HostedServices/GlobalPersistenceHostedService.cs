using BrowserGameEngine.Persistence;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using BrowserGameEngine.StatefulGameServer.GameRegistry;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BrowserGameEngine.FrontendServer {
	public class GlobalPersistenceHostedService : IHostedService, IDisposable {
		private readonly ILogger<GlobalPersistenceHostedService> logger;
		private readonly GlobalPersistenceService globalPersistenceService;
		private readonly GameRegistry gameRegistry;
		private Timer? timer;

		public GlobalPersistenceHostedService(ILogger<GlobalPersistenceHostedService> logger, GlobalPersistenceService globalPersistenceService, GameRegistry gameRegistry) {
			this.logger = logger;
			this.globalPersistenceService = globalPersistenceService;
			this.gameRegistry = gameRegistry;
		}

		public Task StartAsync(CancellationToken stoppingToken) {
			timer = new Timer(DoWork, null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));
			return Task.CompletedTask;
		}

		private void DoWork(object? state) {
			try {
				_ = globalPersistenceService.StoreGlobalState(gameRegistry.GlobalState.ToImmutable());
			} catch (Exception ex) {
				logger.LogError(ex, "GlobalPersistenceHostedService: error storing global state");
			}
		}

		public Task StopAsync(CancellationToken stoppingToken) {
			timer?.Change(Timeout.Infinite, 0);
			return Task.CompletedTask;
		}

		public void Dispose() => timer?.Dispose();
	}
}
