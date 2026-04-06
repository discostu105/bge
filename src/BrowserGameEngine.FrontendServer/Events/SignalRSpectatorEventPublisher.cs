using BrowserGameEngine.FrontendServer.Hubs;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.Events;
using Microsoft.AspNetCore.SignalR;

namespace BrowserGameEngine.FrontendServer.Events;

public class SignalRSpectatorEventPublisher : ISpectatorEventPublisher
{
	private const string SpectatorSnapshotEvent = "SpectatorSnapshot";

	private readonly IHubContext<SpectatorHub> _hubContext;
	private readonly ILogger<SignalRSpectatorEventPublisher> _logger;

	public SignalRSpectatorEventPublisher(
		IHubContext<SpectatorHub> hubContext,
		ILogger<SignalRSpectatorEventPublisher> logger)
	{
		_hubContext = hubContext;
		_logger = logger;
	}

	public void PublishSnapshot(GameId gameId, object snapshot)
	{
		var group = $"spectate:{gameId.Id}";
		_hubContext.Clients.Group(group).SendAsync(SpectatorSnapshotEvent, snapshot)
			.ConfigureAwait(false);
		_logger.LogDebug("Published SpectatorSnapshot for game {GameId}", gameId.Id);
	}
}
