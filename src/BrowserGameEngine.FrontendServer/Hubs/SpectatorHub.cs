using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace BrowserGameEngine.FrontendServer.Hubs;

/// <summary>
/// SignalR hub for anonymous game spectators.
/// Server-to-client only — spectators receive live snapshots but send no messages.
/// Clients pass gameId as a query string parameter on connect.
/// </summary>
[AllowAnonymous]
public class SpectatorHub : Hub
{
	private readonly ILogger<SpectatorHub> _logger;

	public SpectatorHub(ILogger<SpectatorHub> logger)
	{
		_logger = logger;
	}

	public override async Task OnConnectedAsync()
	{
		var gameId = Context.GetHttpContext()?.Request.Query["gameId"].ToString();
		if (!string.IsNullOrEmpty(gameId))
		{
			await Groups.AddToGroupAsync(Context.ConnectionId, $"spectate:{gameId}");
			_logger.LogDebug("Spectator connected for game {GameId}: {ConnectionId}", gameId, Context.ConnectionId);
		}
		else
		{
			_logger.LogWarning("Spectator connected without gameId: {ConnectionId}", Context.ConnectionId);
		}

		await base.OnConnectedAsync();
	}

	public override async Task OnDisconnectedAsync(Exception? exception)
	{
		var gameId = Context.GetHttpContext()?.Request.Query["gameId"].ToString();
		if (!string.IsNullOrEmpty(gameId))
		{
			await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"spectate:{gameId}");
		}

		_logger.LogDebug("Spectator disconnected: {ConnectionId}", Context.ConnectionId);
		await base.OnDisconnectedAsync(exception);
	}
}
