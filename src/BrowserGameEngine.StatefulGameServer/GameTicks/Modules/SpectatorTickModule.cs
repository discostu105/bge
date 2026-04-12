using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.Events;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using System;
using System.Linq;

namespace BrowserGameEngine.StatefulGameServer.GameTicks.Modules;

public class SpectatorTickModule : IGameTickModule
{
	public string Name => "spectator:1";

	private readonly IWorldStateAccessor _worldStateAccessor;
	private readonly GlobalState _globalState;
	private readonly GameDef _gameDef;
	private readonly ISpectatorEventPublisher _spectatorEventPublisher;

	private long _lastPublishedTick = -1;

	public SpectatorTickModule(
		IWorldStateAccessor worldStateAccessor,
		GlobalState globalState,
		GameDef gameDef,
		ISpectatorEventPublisher spectatorEventPublisher)
	{
		_worldStateAccessor = worldStateAccessor;
		_globalState = globalState;
		_gameDef = gameDef;
		_spectatorEventPublisher = spectatorEventPublisher;
	}

	public void SetProperty(string name, string value) { }

	public void CalculateTick(PlayerId playerId)
	{
		var currentTick = _worldStateAccessor.WorldState.GameTickState.CurrentGameTick.Tick;
		if (currentTick == _lastPublishedTick) return;
		_lastPublishedTick = currentTick;

		var gameId = _worldStateAccessor.WorldState.GameId;
		var gameRecord = _globalState.GetGames().FirstOrDefault(g => g.GameId.Id == gameId.Id);
		if (gameRecord == null) return;

		var snapshot = BuildSnapshot(gameId, gameRecord, currentTick);
		_spectatorEventPublisher.PublishSnapshot(gameId, snapshot);
	}

	public object BuildSnapshot(GameId gameId, GameRecordImmutable gameRecord, long tick)
	{
		var landResource = ResourceRepository.LandResource;
		var world = _worldStateAccessor.WorldState;

		var topPlayers = world.Players.Values
			.Select(p => new {
				playerId = p.PlayerId.Id,
				playerName = p.Name,
				land = p.State.Resources.TryGetValue(landResource, out var s) ? s : 0m,
				isOnline = p.LastOnline.HasValue && DateTime.UtcNow - p.LastOnline.Value < TimeSpan.FromMinutes(8),
				isAgent = p.ApiKeyHash != null,
			})
			.OrderByDescending(p => p.land)
			.Take(20)
			.Select((p, i) => new {
				rank = i + 1,
				p.playerId,
				p.playerName,
				p.land,
				p.isOnline,
				p.isAgent,
			})
			.ToList();

		return new {
			gameId = gameId.Id,
			gameName = gameRecord.Name,
			gameStatus = gameRecord.Status.ToString(),
			topPlayers,
			tick,
		};
	}
}
