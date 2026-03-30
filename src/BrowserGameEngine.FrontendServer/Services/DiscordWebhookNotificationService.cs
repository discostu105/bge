using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.GameRegistry;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace BrowserGameEngine.FrontendServer.Services {
	public class DiscordWebhookNotificationService : IGameNotificationService {
		private readonly IHttpClientFactory httpClientFactory;
		private readonly ILogger<DiscordWebhookNotificationService> logger;

		public DiscordWebhookNotificationService(IHttpClientFactory httpClientFactory, ILogger<DiscordWebhookNotificationService> logger) {
			this.httpClientFactory = httpClientFactory;
			this.logger = logger;
		}

		public async Task NotifyGameStartedAsync(GameRecordImmutable record, int playerCount) {
			if (string.IsNullOrEmpty(record.DiscordWebhookUrl)) return;
			await PostEmbedAsync(record.DiscordWebhookUrl,
				title: $"Game Started: {record.Name}",
				description: $"{playerCount} player(s) have joined. Good luck!\n\nhttps://ageofagents.net",
				color: 0x57F287);
		}

		public async Task NotifyGameFinishedAsync(GameRecordImmutable record, PlayerId? winnerId, string? winnerName, int playerCount) {
			if (string.IsNullOrEmpty(record.DiscordWebhookUrl)) return;
			var winner = winnerName != null ? $"Winner: **{winnerName}**" : "No winner";
			await PostEmbedAsync(record.DiscordWebhookUrl,
				title: $"Game Over: {record.Name}",
				description: $"{winner}\n\nhttps://ageofagents.net/games/{record.GameId.Id}/results",
				color: 0xED4245);
		}

		private async Task PostEmbedAsync(string webhookUrl, string title, string description, int color) {
			try {
				var client = httpClientFactory.CreateClient();
				var payload = new {
					embeds = new[] {
						new { title, description, color }
					}
				};
				var response = await client.PostAsJsonAsync(webhookUrl, payload);
				if (!response.IsSuccessStatusCode) {
					logger.LogWarning("Discord webhook returned {StatusCode} for {Url}", response.StatusCode, webhookUrl);
				}
			} catch (Exception ex) {
				logger.LogWarning(ex, "Failed to send Discord webhook notification");
			}
		}
	}
}
