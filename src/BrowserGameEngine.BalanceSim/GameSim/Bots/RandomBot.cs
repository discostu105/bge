using System;
using System.Linq;
using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.Commands;

namespace BrowserGameEngine.BalanceSim.GameSim.Bots;

/// <summary>
/// Tiny demo bot: each tick, pick a random valid action. Useful as a baseline opponent and as
/// a fuzz-tester (it will exercise rare command paths). Anyone writing a new bot can read this
/// in five minutes — start here, then graduate to <see cref="ConfigurableBot"/>.
/// </summary>
public class RandomBot : IBot {
	public string Name { get; }
	public string Race { get; }
	private readonly Random rng;

	public RandomBot(string race, int seed = 0) {
		Race = race;
		Name = $"random-{race}";
		rng = new Random(seed);
	}

	public void OnTick(BotContext ctx) {
		// 50% chance to enqueue a unit it can build, 25% to enqueue a building, 25% to do nothing.
		int roll = rng.Next(0, 4);
		if (roll == 0) return;
		if (roll == 1) TryQueueBuilding(ctx);
		else TryQueueUnit(ctx);
	}

	private void TryQueueUnit(BotContext ctx) {
		var available = ctx.Game.UnitRepository.GetUnitsPrerequisitesMet(ctx.PlayerId).ToList();
		if (available.Count == 0) return;
		var pick = available[rng.Next(available.Count)];
		ctx.Game.BuildQueueRepositoryWrite.AddToQueue(
			new AddToQueueCommand(ctx.PlayerId, SimGame.BuildQueueTypeUnit, pick.Id.Id, Count: 1));
	}

	private void TryQueueBuilding(BotContext ctx) {
		var raceId = Id.PlayerType(Race);
		var candidates = ctx.Game.GameDef.GetAssetsByPlayerType(raceId)
			.Where(a => !ctx.Game.AssetRepository.HasAsset(ctx.PlayerId, a.Id))
			.Where(a => ctx.Game.AssetRepository.PrerequisitesMet(ctx.PlayerId, a))
			.ToList();
		if (candidates.Count == 0) return;
		var pick = candidates[rng.Next(candidates.Count)];
		ctx.Game.BuildQueueRepositoryWrite.AddToQueue(
			new AddToQueueCommand(ctx.PlayerId, SimGame.BuildQueueTypeAsset, pick.Id.Id, Count: 1));
	}
}
