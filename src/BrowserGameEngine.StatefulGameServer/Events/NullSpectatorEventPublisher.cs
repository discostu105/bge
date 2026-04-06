using BrowserGameEngine.GameModel;

namespace BrowserGameEngine.StatefulGameServer.Events;

/// <summary>No-op implementation used in tests and when SignalR is not configured.</summary>
public class NullSpectatorEventPublisher : ISpectatorEventPublisher
{
	public static readonly NullSpectatorEventPublisher Instance = new();

	public void PublishSnapshot(GameId gameId, object snapshot) { }
}
