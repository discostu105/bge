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
		private int isactive = 0;
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
			if (Interlocked.CompareExchange(ref isactive, 1, 0) != 0) return;
			try {
				SaveGlobalState().GetAwaiter().GetResult();
			} catch (Exception ex) {
				logger.LogError(ex, "GlobalPersistenceHostedService: error storing global state");
			} finally {
				Interlocked.Exchange(ref isactive, 0);
			}
		}

		private async Task SaveGlobalState() =>
			await globalPersistenceService.StoreGlobalState(gameRegistry.GlobalState.ToImmutable());

		public async Task StopAsync(CancellationToken stoppingToken) {
			timer?.Change(Timeout.Infinite, 0);
			await SaveGlobalState();
		}

		public void Dispose() => timer?.Dispose();
	}
}
