using System;
using System.Collections.Generic;
using System.Linq;
using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.Commands;

namespace BrowserGameEngine.BalanceSim.GameSim.Bots;

/// <summary>
/// A simple, deterministic bot driven by <see cref="BotConfig"/>. Each tick it runs a small set
/// of decisions in priority order:
///   1. assign workers (mineral/gas split)
///   2. queue the next missing building from BuildOrder
///   3. train more workers (up to WorkerTarget)
///   4. colonize land if below LandTarget
///   5. research attack/defense upgrades
///   6. train combat units according to UnitMix
///   7. (after FirstAttackTick) attack the weakest non-protected opponent
/// The bot uses the engine's BuildQueue, so out-of-resource situations are handled by the queue
/// retrying on the next tick instead of the bot crashing.
/// </summary>
public class ConfigurableBot : IBot {
	public string Name { get; }
	public string Race { get; }
	public BotConfig Config { get; }

	/// <summary>Hard cap on enqueued items so a slow front-of-queue entry doesn't make the bot
	/// hoard hundreds of pending units behind it.</summary>
	public int MaxQueueLength { get; init; } = 4;

	/// <summary>Max units per queue entry (engine BuildUnit accepts Count). Larger batches
	/// match how real players queue (a stack of 10 at a time) and avoid a 1-unit-per-tick
	/// throughput cap from the build-queue tick module.</summary>
	public int BatchSize { get; init; } = 10;

	private readonly Dictionary<string, int> lastAttackTickByOpponent = new();
	private int unitMixCursor;

	public ConfigurableBot(string name, string race, BotConfig config, int seed = 0) {
		Name = name;
		Race = race;
		Config = config;
		// Seed shifts the starting point of the unit-mix round-robin so identical bots in a single
		// game don't all build the same unit on the same tick.
		unitMixCursor = seed;
	}

	public void OnTick(BotContext ctx) {
		AdjustWorkers(ctx);
		QueueNextBuilding(ctx);
		TrainWorkers(ctx);
		Colonize(ctx);
		ResearchUpgrades(ctx);
		TrainCombatUnits(ctx);
		MaybeAttack(ctx);
	}

	private string WorkerUnit() => Race switch {
		"terran" => "wbf",
		"zerg" => "drone",
		"protoss" => "probe",
		_ => "wbf"
	};

	private void AdjustWorkers(BotContext ctx) {
		int total = ctx.Game.UnitRepository.CountByUnitDefId(ctx.PlayerId, Id.UnitDef(WorkerUnit()));
		if (total <= 0) return;
		int gas = (int)Math.Round(total * Config.GasShare);
		int minerals = total - gas;
		try {
			ctx.Game.PlayerRepositoryWrite.AssignWorkers(
				new AssignWorkersCommand(ctx.PlayerId, MineralWorkers: minerals, GasWorkers: gas),
				totalWorkers: total);
		} catch (ArgumentOutOfRangeException) { /* total changed concurrently; retry next tick */ }
	}

	private void TrainWorkers(BotContext ctx) {
		var worker = WorkerUnit();
		int have = ctx.Game.UnitRepository.CountByUnitDefId(ctx.PlayerId, Id.UnitDef(worker));
		int queued = CountQueued(ctx, worker);
		int needed = Config.WorkerTarget - have - queued;
		if (needed <= 0) return;
		if (QueueLength(ctx) >= MaxQueueLength) return;
		// Batch up to BatchSize, capped by what we can afford and what's still needed.
		int batch = Math.Min(needed, AffordableUnitCount(ctx, worker, BatchSize));
		if (batch <= 0) return;
		Enqueue(ctx, SimGame.BuildQueueTypeUnit, worker, count: batch);
	}

	private void QueueNextBuilding(BotContext ctx) {
		foreach (var assetId in Config.BuildOrder) {
			var assetDefId = Id.AssetDef(assetId);
			if (ctx.Game.AssetRepository.HasAsset(ctx.PlayerId, assetDefId)) continue;
			if (ctx.Game.AssetRepository.IsBuildQueued(ctx.PlayerId, assetDefId)) return;
			if (IsAssetQueuedInBuildQueue(ctx, assetId)) return;
			if (!CanAffordAsset(ctx, assetId)) return; // avoid head-blocking the queue
			Enqueue(ctx, SimGame.BuildQueueTypeAsset, assetId, count: 1);
			return;
		}
	}

	private static bool CanAffordAsset(BotContext ctx, string assetDefId) {
		var def = ctx.Game.GameDef.GetAssetDef(Id.AssetDef(assetDefId));
		if (def == null) return false;
		return ctx.Game.ResourceRepository.CanAfford(ctx.PlayerId, def.Cost);
	}

	private void Colonize(BotContext ctx) {
		var me = ctx.Me;
		if (me.Land >= Config.LandTarget) return;
		decimal currentLand = me.Land;
		decimal costPerLand = Math.Max(1m, currentLand / 4m);
		int amount = Math.Max(1, me.Minerals / Math.Max(1, (int)costPerLand) / 4);
		amount = Math.Clamp(amount, 1, 24);
		decimal totalCost = amount * costPerLand;
		if (me.Minerals < totalCost + Config.MineralReserve) return;
		try {
			ctx.Game.ColonizeRepositoryWrite.Colonize(new ColonizeCommand(ctx.PlayerId, amount));
		} catch (Exception) { /* retry next tick */ }
	}

	private void ResearchUpgrades(BotContext ctx) {
		if (ctx.CurrentTick < Config.FirstUpgradeTick) return;
		var state = ctx.MyPlayer.State;
		if (state.UpgradeBeingResearched != UpgradeType.None) return;
		var pick = state.AttackUpgradeLevel <= state.DefenseUpgradeLevel ? UpgradeType.Attack : UpgradeType.Defense;
		int level = pick == UpgradeType.Attack ? state.AttackUpgradeLevel : state.DefenseUpgradeLevel;
		if (level >= 3) {
			pick = pick == UpgradeType.Attack ? UpgradeType.Defense : UpgradeType.Attack;
			level = pick == UpgradeType.Attack ? state.AttackUpgradeLevel : state.DefenseUpgradeLevel;
			if (level >= 3) return;
		}
		try {
			ctx.Game.UpgradeRepositoryWrite.ResearchUpgrade(new ResearchUpgradeCommand(ctx.PlayerId, pick));
		} catch (Exception) { /* not affordable yet */ }
	}

	private void TrainCombatUnits(BotContext ctx) {
		if (QueueLength(ctx) >= MaxQueueLength) return;
		var mix = Config.UnitMix ?? BotConfig.DefaultUnitMixFor(Race);
		if (mix.Count == 0) return;
		// Filter to units we can both build (prerequisites met) AND afford at least one of
		// right now. Head-of-queue blocking means a queued unit we can't afford stalls the
		// whole queue.
		var available = mix.Where(kv => CanBuildUnit(ctx, kv.Key) && CanAffordUnit(ctx, kv.Key)).ToList();
		if (available.Count == 0) return;
		var flat = available.SelectMany(kv => Enumerable.Repeat(kv.Key, kv.Value)).ToList();
		if (flat.Count == 0) return;
		var unit = flat[unitMixCursor++ % flat.Count];
		// Batch by how many we can afford up to BatchSize. The engine's BuildUnit handles
		// Count > 1 atomically, so this matches what a real player would do (queue a stack
		// of 10 marines instead of 1).
		int batch = AffordableUnitCount(ctx, unit, BatchSize);
		if (batch <= 0) return;
		Enqueue(ctx, SimGame.BuildQueueTypeUnit, unit, count: batch);
	}

	private static bool CanAffordUnit(BotContext ctx, string unitDefId) {
		var def = ctx.Game.GameDef.GetUnitDef(Id.UnitDef(unitDefId));
		if (def == null) return false;
		return ctx.Game.ResourceRepository.CanAfford(ctx.PlayerId, def.Cost);
	}

	/// <summary>How many of a unit can we afford right now, capped at <paramref name="cap"/>?</summary>
	private static int AffordableUnitCount(BotContext ctx, string unitDefId, int cap) {
		var def = ctx.Game.GameDef.GetUnitDef(Id.UnitDef(unitDefId));
		if (def == null) return 0;
		int max = cap;
		foreach (var (resId, perUnit) in def.Cost.Resources) {
			if (perUnit <= 0) continue;
			var have = ctx.Game.ResourceRepository.GetAmount(ctx.PlayerId, resId);
			int affordable = (int)(have / perUnit);
			if (affordable < max) max = affordable;
		}
		return Math.Max(0, max);
	}

	private void MaybeAttack(BotContext ctx) {
		if (ctx.CurrentTick < Config.FirstAttackTick) return;
		var me = ctx.Me;
		if (me.ArmyStrength < Config.AttackArmyStrengthThreshold) return;

		var workerId = Id.UnitDef(WorkerUnit());
		var myUnits = ctx.MyPlayer.State.Units;
		bool hasMobileArmy = myUnits.Any(u =>
			!u.UnitDefId.Equals(workerId)
			&& u.Position == null
			&& (ctx.Game.GameDef.GetUnitDef(u.UnitDefId)?.IsMobile ?? false));
		if (!hasMobileArmy) return;

		var targets = ctx.Opponents()
			.Where(o => o.ProtectionTicksRemaining == 0)
			.Where(o => ctx.Game.PlayerRepository.IsPlayerAttackable(ctx.PlayerId, o.PlayerId))
			.Where(o => !RecentlyAttacked(o.PlayerId, ctx.CurrentTick))
			.OrderBy(o => o.ArmyStrength)
			.ToList();
		if (targets.Count == 0) return;
		var target = targets[0];

		bool sent = false;
		// Capture units before any mutation to avoid iteration issues.
		var combatUnits = myUnits
			.Where(u => !u.UnitDefId.Equals(workerId))
			.Where(u => u.Position == null)
			.Where(u => ctx.Game.GameDef.GetUnitDef(u.UnitDefId)?.IsMobile ?? false)
			.ToList();
		foreach (var u in combatUnits) {
			try {
				ctx.Game.UnitRepositoryWrite.SendUnit(new SendUnitCommand(ctx.PlayerId, u.UnitId, target.PlayerId));
				sent = true;
			} catch (Exception) { /* skip individual unit */ }
		}
		if (!sent) return;

		try {
			ctx.Game.UnitRepositoryWrite.Attack(ctx.PlayerId, target.PlayerId);
			lastAttackTickByOpponent[target.PlayerId.Id] = ctx.CurrentTick;
		} catch (Exception) {
			try { ctx.Game.UnitRepositoryWrite.ReturnUnitsHome(new ReturnUnitsHomeCommand(ctx.PlayerId, target.PlayerId)); } catch { }
		}
	}

	// -- helpers ----------------------------------------------------------

	private static bool CanBuildUnit(BotContext ctx, string unitDefId) {
		var def = ctx.Game.GameDef.GetUnitDef(Id.UnitDef(unitDefId));
		if (def == null) return false;
		return ctx.Game.UnitRepository.PrerequisitesMet(ctx.PlayerId, def);
	}

	private bool RecentlyAttacked(PlayerId target, int currentTick) {
		if (!lastAttackTickByOpponent.TryGetValue(target.Id, out int last)) return false;
		return currentTick - last < Config.AttackCooldownTicks;
	}

	private static void Enqueue(BotContext ctx, string type, string defId, int count) {
		ctx.Game.BuildQueueRepositoryWrite.AddToQueue(new AddToQueueCommand(ctx.PlayerId, type, defId, count));
	}

	private static int QueueLength(BotContext ctx) {
		var q = ctx.MyPlayer.State.BuildQueue;
		return q?.Count ?? 0;
	}

	private static int CountQueued(BotContext ctx, string defId) {
		var q = ctx.MyPlayer.State.BuildQueue;
		if (q == null) return 0;
		return q.Where(e => e.DefId == defId).Sum(e => e.Count);
	}

	private static bool IsAssetQueuedInBuildQueue(BotContext ctx, string assetDefId) {
		var q = ctx.MyPlayer.State.BuildQueue;
		if (q == null) return false;
		return q.Any(e => e.Type == SimGame.BuildQueueTypeAsset && e.DefId == assetDefId);
	}
}
