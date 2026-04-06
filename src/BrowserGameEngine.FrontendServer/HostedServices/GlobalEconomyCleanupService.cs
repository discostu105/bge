using BrowserGameEngine.StatefulGameServer.GameRegistry;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BrowserGameEngine.FrontendServer.HostedServices;

public class GlobalEconomyCleanupService : BackgroundService {
	private readonly CurrencyService currencyService;
	private readonly ILogger<GlobalEconomyCleanupService> logger;

	public GlobalEconomyCleanupService(CurrencyService currencyService, ILogger<GlobalEconomyCleanupService> logger) {
		this.currencyService = currencyService;
		this.logger = logger;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
		while (!stoppingToken.IsCancellationRequested) {
			try {
				await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
				currencyService.ExpireTradeOffers(DateTime.UtcNow);
			} catch (OperationCanceledException) {
				break;
			} catch (Exception ex) {
				logger.LogError(ex, "Error during economy cleanup");
			}
		}
	}
}
