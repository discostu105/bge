using BrowserGameEngine.FrontendServer.Hubs;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer;
using BrowserGameEngine.StatefulGameServer.Events;
using Microsoft.AspNetCore.SignalR;

namespace BrowserGameEngine.FrontendServer.Events;

/// <summary>
/// SignalR-backed implementation of IGameEventPublisher.
/// Sends real-time events to connected clients via the GameHub.
/// </summary>
public class SignalRGameEventPublisher : IGameEventPublisher
{
	private readonly IHubContext<GameHub> _hubContext;
	private readonly PlayerConnectionTracker _tracker;
	private readonly AllianceRepository _allianceRepository;
	private readonly ILogger<SignalRGameEventPublisher> _logger;

	public SignalRGameEventPublisher(
		IHubContext<GameHub> hubContext,
		PlayerConnectionTracker tracker,
		AllianceRepository allianceRepository,
		ILogger<SignalRGameEventPublisher> logger)
	{
		_hubContext = hubContext;
		_tracker = tracker;
		_allianceRepository = allianceRepository;
		_logger = logger;
	}

	public void PublishToPlayer(PlayerId playerId, string eventType, object payload)
	{
		var connectionIds = _tracker.GetConnections(playerId);
		if (connectionIds.Count == 0) return;

		_hubContext.Clients.Clients(connectionIds).SendAsync(eventType, payload)
			.ConfigureAwait(false);
		_logger.LogDebug("Published {EventType} to player {PlayerId} ({Count} connections)",
			eventType, playerId.Id, connectionIds.Count);
	}

	public void PublishToAlliance(AllianceId allianceId, string eventType, object payload)
	{
		var alliance = _allianceRepository.Get(allianceId);
		if (alliance == null) return;

		var connectionIds = new List<string>();
		foreach (var member in alliance.Members.Where(m => !m.IsPending)) {
			connectionIds.AddRange(_tracker.GetConnections(member.PlayerId));
		}
		if (connectionIds.Count == 0) return;

		_hubContext.Clients.Clients(connectionIds).SendAsync(eventType, payload)
			.ConfigureAwait(false);
		_logger.LogDebug("Published {EventType} to alliance {AllianceId} ({Count} connections)",
			eventType, allianceId.Id, connectionIds.Count);
	}

	public void PublishToGame(string eventType, object payload)
	{
		var connectionIds = _tracker.GetAllConnectionIds();
		if (connectionIds.Count == 0) return;

		_hubContext.Clients.Clients(connectionIds).SendAsync(eventType, payload)
			.ConfigureAwait(false);
		_logger.LogDebug("Published {EventType} to all ({Count} connections)",
			eventType, connectionIds.Count);
	}
}
