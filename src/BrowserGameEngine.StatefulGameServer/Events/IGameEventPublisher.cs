using BrowserGameEngine.GameModel;

namespace BrowserGameEngine.StatefulGameServer.Events;

/// <summary>
/// Publishes real-time game events to connected clients.
/// Implementations may use SignalR, no-op (tests), or other transports.
/// </summary>
public interface IGameEventPublisher
{
	void PublishToPlayer(PlayerId playerId, string eventType, object payload);
	void PublishToAlliance(AllianceId allianceId, string eventType, object payload);
	void PublishToGame(string eventType, object payload);
}
