using System.Collections.Generic;
using BrowserGameEngine.GameModel;

namespace BrowserGameEngine.BalanceSim.GameSim;

/// <summary>
/// A bot decides what its player should do each game tick. Bots mutate the game state through
/// the <see cref="BotContext"/> (which exposes the same repositories the real game server uses)
/// rather than returning command lists, so error handling and timing match production.
/// </summary>
public interface IBot {
	/// <summary>Short label used in reports (e.g. "rush-terran").</summary>
	string Name { get; }

	/// <summary>Race / player type id this bot was built for ("terran", "zerg", "protoss").</summary>
	string Race { get; }

	/// <summary>Called once per game tick, after all engine modules have run for that tick.</summary>
	void OnTick(BotContext ctx);
}

/// <summary>Wraps a SimGame for a single player so bots can issue commands without touching other players.</summary>
public class BotContext {
	public SimGame Game { get; }
	public PlayerId PlayerId { get; }

	public BotContext(SimGame game, PlayerId playerId) {
		Game = game;
		PlayerId = playerId;
	}

	public int CurrentTick => Game.CurrentTick;

	public PlayerSnapshot Me => Game.GetSnapshot(PlayerId);

	/// <summary>Latest immutable view of the player (units, assets, resources, build queue, upgrades).</summary>
	public PlayerImmutable MyPlayer => Game.PlayerRepository.Get(PlayerId);

	/// <summary>All other players in the game, regardless of attackability.</summary>
	public IEnumerable<PlayerSnapshot> Opponents() {
		foreach (var p in Game.Players) {
			if (p == PlayerId) continue;
			yield return Game.GetSnapshot(p);
		}
	}
}
