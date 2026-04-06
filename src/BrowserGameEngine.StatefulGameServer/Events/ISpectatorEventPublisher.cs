using BrowserGameEngine.GameModel;

namespace BrowserGameEngine.StatefulGameServer.Events;

/// <summary>
/// Publishes real-time spectator snapshots to anonymous viewers watching a game.
/// Implementations may use SignalR, no-op (tests), or other transports.
/// </summary>
public interface ISpectatorEventPublisher
{
	void PublishSnapshot(GameId gameId, object snapshot);
}
