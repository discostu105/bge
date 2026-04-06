using BrowserGameEngine.GameModel;
using BrowserGameEngine.Persistence;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using BrowserGameEngine.StatefulGameServer.GameRegistry;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BrowserGameEngine.StatefulGameServer;

public class GameReplayData {
	public GameRecordImmutable? Record { get; init; }
	public IReadOnlyList<PlayerAchievementImmutable> GameAchievements { get; init; } = [];
	public PlayerAchievementImmutable? CurrentPlayerAchievement { get; init; }
	public WorldStateImmutable? WorldState { get; init; }
}

public class GameReplayRepository {
	private readonly GlobalState globalState;
	private readonly GameRegistry.GameRegistry gameRegistry;
	private readonly PersistenceService persistenceService;

	public GameReplayRepository(GlobalState globalState, GameRegistry.GameRegistry gameRegistry, PersistenceService persistenceService) {
		this.globalState = globalState;
		this.gameRegistry = gameRegistry;
		this.persistenceService = persistenceService;
	}

	public async Task<GameReplayData?> GetGameReplayData(GameId gameId, string userId) {
		var record = null as GameRecordImmutable;
		foreach (var g in globalState.GetGames()) {
			if (g.GameId.Id == gameId.Id) { record = g; break; }
		}
		if (record == null) return null;

		var allGameAchievements = new List<PlayerAchievementImmutable>();
		PlayerAchievementImmutable? currentPlayerAchievement = null;
		foreach (var a in globalState.GetAchievements()) {
			if (a.GameId.Id == gameId.Id) {
				allGameAchievements.Add(a);
				if (a.UserId == userId) currentPlayerAchievement = a;
			}
		}

		WorldStateImmutable? worldState = null;
		var activeInstance = gameRegistry.TryGetInstance(gameId);
		if (activeInstance != null) {
			worldState = activeInstance.WorldState.ToImmutable();
		} else if (persistenceService.GameStateExists(gameId)) {
			worldState = await persistenceService.LoadGameState(gameId);
		}

		return new GameReplayData {
			Record = record,
			GameAchievements = allGameAchievements,
			CurrentPlayerAchievement = currentPlayerAchievement,
			WorldState = worldState
		};
	}
}
