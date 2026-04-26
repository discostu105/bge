using System;
using System.Collections.Generic;
using System.Linq;
using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;

namespace BrowserGameEngine.BalanceSim.GameSim;

/// <summary>
/// Drives a SimGame to completion with a fixed set of bots. Exposes optional per-tick callbacks
/// so callers (CLI, tests, notebooks) can capture intermediate state without changing the runner.
/// </summary>
public class PlaythroughRunner {
	public GameDef? GameDefOverride { get; init; }
	public GameSettings Settings { get; init; } = GameSettings.Default;
	public Action<int, SimGame>? OnTick { get; init; }
	public Action<string>? OnLog { get; init; }

	public PlaythroughResult Run(IReadOnlyList<IBot> bots) {
		if (bots.Count == 0) throw new ArgumentException("At least one bot is required.", nameof(bots));
		if (Settings.EndTick <= 0) throw new ArgumentException("GameSettings.EndTick must be positive for a finite simulation.", nameof(Settings));

		var game = new SimGame(GameDefOverride, Settings);
		var contexts = new List<(IBot Bot, BotContext Ctx)>();
		foreach (var bot in bots) {
			var pid = game.AddPlayer(bot.Name, bot.Race);
			contexts.Add((bot, new BotContext(game, pid)));
		}

		OnLog?.Invoke($"Game start: {bots.Count} bots, EndTick={Settings.EndTick}, ProtectionTicks={Settings.ProtectionTicks}");

		var startWall = DateTime.UtcNow;
		while (!game.IsGameOver) {
			game.AdvanceTicks(1);
			foreach (var (bot, ctx) in contexts) {
				try {
					bot.OnTick(ctx);
				} catch (Exception ex) {
					OnLog?.Invoke($"Bot '{bot.Name}' threw at tick {game.CurrentTick}: {ex.GetType().Name}: {ex.Message}");
				}
			}
			OnTick?.Invoke(game.CurrentTick, game);
		}
		var elapsed = DateTime.UtcNow - startWall;

		var ranking = game.Ranking();
		var winner = ranking[0];
		OnLog?.Invoke($"Game over at tick {game.CurrentTick}. Winner: {winner.Name} ({winner.Race}) — land={winner.Land} m+g={winner.Minerals + winner.Gas}");

		return new PlaythroughResult(
			Settings: Settings,
			TicksRun: game.CurrentTick,
			ElapsedWall: elapsed,
			Ranking: ranking.ToList(),
			BotNamesByPlayer: contexts.ToDictionary(t => t.Ctx.PlayerId, t => t.Bot.Name)
		);
	}
}

public record PlaythroughResult(
	GameSettings Settings,
	int TicksRun,
	TimeSpan ElapsedWall,
	IReadOnlyList<PlayerSnapshot> Ranking,
	IReadOnlyDictionary<PlayerId, string> BotNamesByPlayer
) {
	public PlayerSnapshot Winner => Ranking[0];
	public string WinnerName => BotNamesByPlayer[Winner.PlayerId];
	public string WinnerRace => Winner.Race;
}
