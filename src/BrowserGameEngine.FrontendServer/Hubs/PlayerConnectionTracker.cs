using System.Collections.Concurrent;
using BrowserGameEngine.GameModel;

namespace BrowserGameEngine.FrontendServer.Hubs;

/// <summary>
/// Thread-safe singleton that maps player IDs to active SignalR connection IDs.
/// A player may have multiple connections (multiple browser tabs).
/// </summary>
public class PlayerConnectionTracker
{
	private readonly ConcurrentDictionary<string, HashSet<string>> _playerConnections = new();
	private readonly ConcurrentDictionary<string, string> _connectionToPlayer = new();

	public void Track(PlayerId playerId, string connectionId)
	{
		var key = playerId.Id;
		_connectionToPlayer[connectionId] = key;
		_playerConnections.AddOrUpdate(
			key,
			_ => new HashSet<string> { connectionId },
			(_, existing) => {
				lock (existing) {
					existing.Add(connectionId);
				}
				return existing;
			}
		);
	}

	public void Untrack(string connectionId)
	{
		if (!_connectionToPlayer.TryRemove(connectionId, out var playerId)) return;
		if (!_playerConnections.TryGetValue(playerId, out var connections)) return;
		lock (connections) {
			connections.Remove(connectionId);
			if (connections.Count == 0) {
				_playerConnections.TryRemove(playerId, out _);
			}
		}
	}

	public IReadOnlyList<string> GetConnections(PlayerId playerId)
	{
		if (!_playerConnections.TryGetValue(playerId.Id, out var connections)) return [];
		lock (connections) {
			return connections.ToList();
		}
	}

	public IReadOnlyList<string> GetAllConnectionIds()
	{
		return _connectionToPlayer.Keys.ToList();
	}
}
