using BrowserGameEngine.GameModel;

namespace BrowserGameEngine.StatefulGameServer.Events;

/// <summary>No-op implementation used in tests and when SignalR is not configured.</summary>
public class NullGameEventPublisher : IGameEventPublisher
{
	public static readonly NullGameEventPublisher Instance = new();

	public void PublishToPlayer(PlayerId playerId, string eventType, object payload) { }
	public void PublishToAlliance(AllianceId allianceId, string eventType, object payload) { }
	public void PublishToGame(string eventType, object payload) { }
}
