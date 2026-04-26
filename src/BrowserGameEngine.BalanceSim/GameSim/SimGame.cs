using System;
using System.Collections.Generic;
using System.Linq;
using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameDefinition.SCO;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer;
using BrowserGameEngine.StatefulGameServer.ActionFeed;
using BrowserGameEngine.StatefulGameServer.Commands;
using BrowserGameEngine.StatefulGameServer.Events;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using BrowserGameEngine.StatefulGameServer.GameRegistry;
using BrowserGameEngine.StatefulGameServer.GameTicks;
using BrowserGameEngine.StatefulGameServer.GameTicks.Modules;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace BrowserGameEngine.BalanceSim.GameSim;

/// <summary>
/// Headless game harness for full-game simulations. Wires the SCO engine without the web layer.
/// The <c>gamefinalization:1</c> tick module is dropped (it depends on GlobalState/GameLifecycleEngine);
/// the runner detects end conditions itself.
/// </summary>
public class SimGame {
	/// <summary>BuildQueue entry-type constants. Mirrors the engine's internal constants so bots
	/// in this assembly can build commands without depending on internals.</summary>
	public const string BuildQueueTypeUnit = "unit";
	public const string BuildQueueTypeAsset = "asset";

	public GameDef GameDef { get; }
	public GameSettings Settings { get; }
	public IWorldStateAccessor Accessor { get; }
	public GameTickEngine TickEngine { get; }

	public PlayerRepository PlayerRepository { get; }
	public PlayerRepositoryWrite PlayerRepositoryWrite { get; }
	public ResourceRepository ResourceRepository { get; }
	public ResourceRepositoryWrite ResourceRepositoryWrite { get; }
	public ColonizeRepositoryWrite ColonizeRepositoryWrite { get; }
	public AssetRepository AssetRepository { get; }
	public AssetRepositoryWrite AssetRepositoryWrite { get; }
	public ActionQueueRepository ActionQueueRepository { get; }
	public UnitRepository UnitRepository { get; }
	public UnitRepositoryWrite UnitRepositoryWrite { get; }
	public BuildQueueRepository BuildQueueRepository { get; }
	public BuildQueueRepositoryWrite BuildQueueRepositoryWrite { get; }
	public UpgradeRepository UpgradeRepository { get; }
	public UpgradeRepositoryWrite UpgradeRepositoryWrite { get; }

	private readonly List<PlayerId> playerOrder = new();
	public IReadOnlyList<PlayerId> Players => playerOrder;

	public int CurrentTick => Accessor.WorldState.GameTickState.CurrentGameTick.Tick;

	public SimGame(GameDef? gameDef = null, GameSettings? settings = null) {
		GameDef = StripFinalizationModule(gameDef ?? new StarcraftOnlineGameDefFactory().CreateGameDef());
		Settings = settings ?? GameSettings.Default;

		// Build a minimal WorldState. LastUpdate is set far in the future so the engine never
		// considers a tick "due" on its wall-clock — we drive ticks manually via IncrementWorldTick.
		var initial = new WorldStateImmutable(
			Players: new Dictionary<PlayerId, PlayerImmutable>(),
			GameTickState: new GameTickStateImmutable(new GameTick(0), DateTime.UtcNow.AddYears(100)),
			GameActionQueue: new List<GameActionImmutable>(),
			GameId: new GameId("sim")
		);
		var mutable = initial.ToMutable();

		// Use GameInstance to assign GameSettings (internal field) via its public constructor.
		var record = new GameRecordImmutable(
			GameId: mutable.GameId,
			Name: "sim",
			GameDefType: "sco",
			Status: GameStatus.Active,
			StartTime: DateTime.UtcNow,
			EndTime: DateTime.UtcNow.AddDays(365),
			TickDuration: GameDef.TickDuration,
			Settings: Settings
		);
		var instance = new GameInstance(record, mutable, GameDef);
		Accessor = instance.WorldStateAccessor;

		var loggerFactory = new NullLoggerFactory();
		var allianceRepository = new AllianceRepository(Accessor);
		ResourceRepository = new ResourceRepository(Accessor, GameDef);
		PlayerRepository = new PlayerRepository(Accessor, ResourceRepository, allianceRepository);
		PlayerRepositoryWrite = new PlayerRepositoryWrite(Accessor, TimeProvider.System);
		ResourceRepositoryWrite = new ResourceRepositoryWrite(Accessor, ResourceRepository, GameDef);
		ColonizeRepositoryWrite = new ColonizeRepositoryWrite(ResourceRepository, ResourceRepositoryWrite);
		ActionQueueRepository = new ActionQueueRepository(Accessor);
		AssetRepository = new AssetRepository(Accessor, PlayerRepository, ActionQueueRepository);
		AssetRepositoryWrite = new AssetRepositoryWrite(loggerFactory.CreateLogger<AssetRepositoryWrite>(), Accessor, AssetRepository, ResourceRepository, ResourceRepositoryWrite, ActionQueueRepository, GameDef);
		UnitRepository = new UnitRepository(Accessor, GameDef, PlayerRepository, AssetRepository);
		UpgradeRepository = new UpgradeRepository(Accessor);
		UpgradeRepositoryWrite = new UpgradeRepositoryWrite(Accessor, ResourceRepository, ResourceRepositoryWrite);
		var battleBehavior = new BattleBehaviorScoOriginal(loggerFactory.CreateLogger<IBattleBehavior>());
		UnitRepositoryWrite = new UnitRepositoryWrite(loggerFactory.CreateLogger<UnitRepositoryWrite>(), Accessor, GameDef, UnitRepository, ResourceRepositoryWrite, ResourceRepository, PlayerRepository, PlayerRepositoryWrite, battleBehavior, UpgradeRepository);
		BuildQueueRepository = new BuildQueueRepository(Accessor);
		BuildQueueRepositoryWrite = new BuildQueueRepositoryWrite(loggerFactory.CreateLogger<BuildQueueRepositoryWrite>(), Accessor, BuildQueueRepository, AssetRepository, AssetRepositoryWrite, UnitRepository, UnitRepositoryWrite, ResourceRepository, GameDef);
		var resourceHistoryRepositoryWrite = new ResourceHistoryRepositoryWrite(Accessor);

		var services = new ServiceCollection();
		services.AddSingleton<IGameTickModule>(new ActionQueueExecutor(AssetRepositoryWrite));
		services.AddSingleton<IGameTickModule>(new ResourceGrowthSco(loggerFactory.CreateLogger<ResourceGrowthSco>(), GameDef, ResourceRepository, ResourceRepositoryWrite, PlayerRepository, PlayerRepositoryWrite, UnitRepository, UnitRepositoryWrite, new ActionLogger()));
		services.AddSingleton<IGameTickModule>(new ResourceHistoryModule(ResourceRepository, resourceHistoryRepositoryWrite, Accessor));
		services.AddSingleton<IGameTickModule>(new NewPlayerProtectionModule(Accessor.WorldState));
		services.AddSingleton<IGameTickModule>(new UpgradeTimer(UpgradeRepositoryWrite));
		services.AddSingleton<IGameTickModule>(new BuildQueueModule(BuildQueueRepositoryWrite));
		services.AddSingleton<IGameTickModule>(new UnitReturn(UnitRepositoryWrite));

		var registry = new GameTickModuleRegistry(loggerFactory.CreateLogger<GameTickModuleRegistry>(), services.BuildServiceProvider(), GameDef);
		TickEngine = new GameTickEngine(loggerFactory.CreateLogger<GameTickEngine>(), Accessor, GameDef, registry, PlayerRepositoryWrite, TimeProvider.System, NullGameEventPublisher.Instance);
		// Pause the engine permanently so its wall-clock auto-tick logic never fires; we drive
		// ticks ourselves via AdvanceTicks → IncrementWorldTick + CheckAllTicks.
		TickEngine.PauseTicks();
	}

	/// <summary>Add a player (race) to the game and return its id.</summary>
	public PlayerId AddPlayer(string name, string race) {
		var id = PlayerIdFactory.Create($"sim_{playerOrder.Count}_{name}");
		PlayerRepositoryWrite.CreatePlayer(id, userId: null, playerType: race, protectionTicks: Settings.ProtectionTicks);
		// CreatePlayer always grants "commandcenter" (a Terran building). For Zerg/Protoss,
		// also grant their race-appropriate HQ so a bot can actually start producing workers.
		var starterHq = race switch {
			"zerg" => "hive",
			"protoss" => "nexus",
			_ => null
		};
		if (starterHq != null) {
			AssetRepositoryWrite.GrantBuilding(id, Id.AssetDef(starterHq));
		}
		playerOrder.Add(id);
		return id;
	}

	/// <summary>
	/// Advance the game by exactly <paramref name="ticks"/> world ticks. Each iteration
	/// increments the world tick once and runs every per-player module exactly once.
	/// </summary>
	public void AdvanceTicks(int ticks) {
		for (int i = 0; i < ticks; i++) {
			TickEngine.IncrementWorldTick();
			TickEngine.CheckAllTicks();
		}
	}

	/// <summary>True if the game has reached its configured EndTick.</summary>
	public bool IsGameOver => Settings.EndTick > 0 && CurrentTick >= Settings.EndTick;

	/// <summary>
	/// Players ordered by score (land desc, minerals+gas desc, id asc) — same ranking the real
	/// GameLifecycleEngine uses to pick a winner.
	/// </summary>
	public IReadOnlyList<PlayerSnapshot> Ranking() {
		return playerOrder
			.Select(GetSnapshot)
			.OrderByDescending(s => s.Land)
			.ThenByDescending(s => s.Minerals + s.Gas)
			.ThenBy(s => s.PlayerId.Id, StringComparer.Ordinal)
			.ToList();
	}

	public PlayerSnapshot GetSnapshot(PlayerId playerId) {
		var p = PlayerRepository.Get(playerId);
		var land = ResourceRepository.GetAmount(playerId, Id.ResDef("land"));
		var minerals = ResourceRepository.GetAmount(playerId, Id.ResDef("minerals"));
		var gas = ResourceRepository.GetAmount(playerId, Id.ResDef("gas"));
		var unitCount = p.State.Units.Sum(u => u.Count);
		var armyStrength = p.State.Units.Sum(u => {
			var def = GameDef.GetUnitDef(u.UnitDefId);
			if (def == null) return 0L;
			return (long)(def.Attack + def.Defense) * u.Count;
		});
		return new PlayerSnapshot(
			PlayerId: playerId,
			Name: p.Name,
			Race: p.PlayerType.Id,
			Land: (int)land,
			Minerals: (int)minerals,
			Gas: (int)gas,
			AssetCount: p.State.Assets.Count,
			UnitCount: unitCount,
			ArmyStrength: armyStrength,
			ProtectionTicksRemaining: p.State.ProtectionTicksRemaining,
			AttackUpgradeLevel: p.State.AttackUpgradeLevel,
			DefenseUpgradeLevel: p.State.DefenseUpgradeLevel
		);
	}

	private static GameDef StripFinalizationModule(GameDef src) {
		var modules = src.GameTickModules.Where(m => m.Name != "gamefinalization:1").ToList();
		return new GameDef {
			PlayerTypes = src.PlayerTypes,
			Units = src.Units,
			Assets = src.Assets,
			Resources = src.Resources,
			GameTickModules = modules,
			TickDuration = src.TickDuration,
		};
	}
}

/// <summary>Compact snapshot of a player for ranking, reporting, and bot decisions.</summary>
public record PlayerSnapshot(
	PlayerId PlayerId,
	string Name,
	string Race,
	int Land,
	int Minerals,
	int Gas,
	int AssetCount,
	int UnitCount,
	long ArmyStrength,
	int ProtectionTicksRemaining,
	int AttackUpgradeLevel,
	int DefenseUpgradeLevel
);
