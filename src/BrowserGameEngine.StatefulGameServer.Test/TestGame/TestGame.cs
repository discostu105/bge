using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameDefinition.SCO;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using BrowserGameEngine.StatefulGameServer.GameTicks;
using BrowserGameEngine.StatefulGameServer.GameTicks.Modules;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using BrowserGameEngine.GameModel;

namespace BrowserGameEngine.StatefulGameServer.Test {
	internal class TestGame {
		public NullLoggerFactory LoggerFactory { get; }
		public WorldState World { get; }
		public GameDef GameDef { get; }
		public ScoreRepository ScoreRepository { get; }
		public PlayerRepository PlayerRepository { get; }
		public PlayerRepositoryWrite PlayerRepositoryWrite { get; }
		public ResourceRepository ResourceRepository { get; }
		public ResourceRepositoryWrite ResourceRepositoryWrite { get; }
		public AssetRepository AssetRepository { get; }
		public ActionQueueRepository ActionQueueRepository { get; }
		public AssetRepositoryWrite AssetRepositoryWrite { get; }
		public UnitRepository UnitRepository { get; }
		public IBattleBehavior BattleBehavior { get; }
		public UnitRepositoryWrite UnitRepositoryWrite { get; }
		public TestWorldStateFactory WorldStateFactory { get; }
		public GameTickModuleRegistry GameTickModuleRegistry { get; }
		public GameTickEngine TickEngine { get; }

		public PlayerId Player1 => WorldStateFactory.Player1;

		public TestGame() {
			LoggerFactory = new Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory();
			WorldStateFactory = new TestWorldStateFactory();
			World = WorldStateFactory.CreateDevWorldState().ToMutable();
			GameDef = new TestGameDefFactory().CreateGameDef();
			ScoreRepository = new ScoreRepository(GameDef, World);
			PlayerRepository = new PlayerRepository(World, ScoreRepository);
			PlayerRepositoryWrite = new PlayerRepositoryWrite(World);
			ResourceRepository = new ResourceRepository(World);
			ResourceRepositoryWrite = new ResourceRepositoryWrite(World, ResourceRepository);
			ActionQueueRepository = new ActionQueueRepository(World);
			AssetRepository = new AssetRepository(World, PlayerRepository, ActionQueueRepository);
			AssetRepositoryWrite = new AssetRepositoryWrite(LoggerFactory.CreateLogger<AssetRepositoryWrite>(), World, AssetRepository, ResourceRepository, ResourceRepositoryWrite, ActionQueueRepository, GameDef);
			UnitRepository = new UnitRepository(World, GameDef, PlayerRepository, AssetRepository);
			BattleBehavior = new BattleBehaviorScoOriginal(LoggerFactory.CreateLogger<IBattleBehavior>());
			UnitRepositoryWrite = new UnitRepositoryWrite(LoggerFactory.CreateLogger<UnitRepositoryWrite>(), World, GameDef, UnitRepository, ResourceRepositoryWrite, PlayerRepository, BattleBehavior);

			var services = new ServiceCollection();
			services.AddSingleton<IGameTickModule>(new ActionQueueExecutor(AssetRepositoryWrite));
			services.AddSingleton<IGameTickModule>(new ResourceGrowthSco(LoggerFactory.CreateLogger<ResourceGrowthSco>(), GameDef, ResourceRepository, ResourceRepositoryWrite, AssetRepository, UnitRepository));
			GameTickModuleRegistry = new GameTickModuleRegistry(LoggerFactory.CreateLogger<GameTickModuleRegistry>(), services.BuildServiceProvider(), GameDef);
			TickEngine = new GameTickEngine(LoggerFactory.CreateLogger<GameTickEngine>(), World, GameDef, GameTickModuleRegistry, PlayerRepositoryWrite);
		}
	}
}
