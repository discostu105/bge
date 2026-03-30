using System;
using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameDefinition.SCO;
using BrowserGameEngine.StatefulGameServer.ActionFeed;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using BrowserGameEngine.StatefulGameServer.GameTicks;
using BrowserGameEngine.StatefulGameServer.GameTicks.Modules;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using BrowserGameEngine.GameModel;

namespace BrowserGameEngine.StatefulGameServer.Test {
	public class TestGame {
		public NullLoggerFactory LoggerFactory { get; }
		public WorldState World { get; }
		public GameDef GameDef { get; }
		public GlobalState GlobalState { get; }
		public IWorldStateAccessor Accessor { get; }
		public ScoreRepository ScoreRepository { get; }
		public AllianceRepository AllianceRepository { get; }
		public AllianceRepositoryWrite AllianceRepositoryWrite { get; }
		public PlayerRepository PlayerRepository { get; }
		public PlayerRepositoryWrite PlayerRepositoryWrite { get; }
		public ResourceRepository ResourceRepository { get; }
		public ResourceRepositoryWrite ResourceRepositoryWrite { get; }
		public AssetRepository AssetRepository { get; }
		public ActionQueueRepository ActionQueueRepository { get; }
		public AssetRepositoryWrite AssetRepositoryWrite { get; }
		public UnitRepository UnitRepository { get; }
		public IBattleBehavior BattleBehavior { get; }
		public UpgradeRepository UpgradeRepository { get; }
		public UnitRepositoryWrite UnitRepositoryWrite { get; }
		public MessageRepository MessageRepository { get; }
		public MessageRepositoryWrite MessageRepositoryWrite { get; }
		public BuildQueueRepository BuildQueueRepository { get; }
		public BuildQueueRepositoryWrite BuildQueueRepositoryWrite { get; }
		public TestWorldStateFactory WorldStateFactory { get; }
		public GameTickModuleRegistry GameTickModuleRegistry { get; }
		public GameTickEngine TickEngine { get; }

		public PlayerId Player1 => WorldStateFactory.Player1;

		public TestGame(int playerCount = 1) : this(new TestWorldStateFactory().CreateDevWorldState(playerCount)) { }

		public TestGame(WorldStateImmutable initialState) {
			LoggerFactory = new Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory();
			WorldStateFactory = new TestWorldStateFactory();
			World = initialState.ToMutable();
			GameDef = new TestGameDefFactory().CreateGameDef();
			GlobalState = new GlobalState();
			Accessor = new SingletonWorldStateAccessor(World);
			ScoreRepository = new ScoreRepository(GameDef, Accessor);
			AllianceRepository = new AllianceRepository(Accessor);
			AllianceRepositoryWrite = new AllianceRepositoryWrite(Accessor, AllianceRepository);
			PlayerRepository = new PlayerRepository(Accessor, ScoreRepository, AllianceRepository);
			PlayerRepositoryWrite = new PlayerRepositoryWrite(Accessor, TimeProvider.System);
			ResourceRepository = new ResourceRepository(Accessor, GameDef);
			ResourceRepositoryWrite = new ResourceRepositoryWrite(Accessor, ResourceRepository, GameDef);
			ActionQueueRepository = new ActionQueueRepository(Accessor);
			AssetRepository = new AssetRepository(Accessor, PlayerRepository, ActionQueueRepository);
			AssetRepositoryWrite = new AssetRepositoryWrite(LoggerFactory.CreateLogger<AssetRepositoryWrite>(), Accessor, AssetRepository, ResourceRepository, ResourceRepositoryWrite, ActionQueueRepository, GameDef);
			UnitRepository = new UnitRepository(Accessor, GameDef, PlayerRepository, AssetRepository);
			BattleBehavior = new BattleBehaviorScoOriginal(LoggerFactory.CreateLogger<IBattleBehavior>());
			UpgradeRepository = new UpgradeRepository(Accessor);
			UnitRepositoryWrite = new UnitRepositoryWrite(LoggerFactory.CreateLogger<UnitRepositoryWrite>(), Accessor, GameDef, UnitRepository, ResourceRepositoryWrite, ResourceRepository, PlayerRepository, PlayerRepositoryWrite, BattleBehavior, UpgradeRepository);
			MessageRepository = new MessageRepository(Accessor);
			MessageRepositoryWrite = new MessageRepositoryWrite(Accessor, TimeProvider.System);
			BuildQueueRepository = new BuildQueueRepository(Accessor);
			BuildQueueRepositoryWrite = new BuildQueueRepositoryWrite(LoggerFactory.CreateLogger<BuildQueueRepositoryWrite>(), Accessor, BuildQueueRepository, AssetRepository, AssetRepositoryWrite, UnitRepository, UnitRepositoryWrite, ResourceRepository, GameDef);

			var services = new ServiceCollection();
			services.AddSingleton<IGameTickModule>(new ActionQueueExecutor(AssetRepositoryWrite));
			services.AddSingleton<IGameTickModule>(new ResourceGrowthSco(LoggerFactory.CreateLogger<ResourceGrowthSco>(), GameDef, ResourceRepository, ResourceRepositoryWrite, PlayerRepository, PlayerRepositoryWrite, UnitRepository, UnitRepositoryWrite, new ActionLogger()));
			GameTickModuleRegistry = new GameTickModuleRegistry(LoggerFactory.CreateLogger<GameTickModuleRegistry>(), services.BuildServiceProvider(), GameDef);
			TickEngine = new GameTickEngine(LoggerFactory.CreateLogger<GameTickEngine>(), Accessor, GameDef, GameTickModuleRegistry, PlayerRepositoryWrite, TimeProvider.System);
		}
	}
}
