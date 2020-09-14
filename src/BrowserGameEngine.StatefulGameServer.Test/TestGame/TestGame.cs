using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameDefinition.SCO;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using BrowserGameEngine.StatefulGameServer.GameTicks;
using BrowserGameEngine.StatefulGameServer.GameTicks.Modules;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;

namespace BrowserGameEngine.StatefulGameServer.Test {
	internal class TestGame {
		public NullLoggerFactory LoggerFactory { get; }
		public WorldState World { get; }
		public GameDef GameDef { get; }
		public PlayerRepository PlayerRepository { get; }
		public PlayerRepositoryWrite PlayerRepositoryWrite { get; }
		public ResourceRepository ResourceRepository { get; }
		public ResourceRepositoryWrite ResourceRepositoryWrite { get; }
		public AssetRepository AssetRepository { get; }
		public ActionQueueRepository ActionQueueRepository { get; }
		public AssetRepositoryWrite AssetRepositoryWrite { get; }
		public UnitRepository UnitRepository { get; }
		public UnitRepositoryWrite UnitRepositoryWrite { get; }
		public TestWorldStateFactory WorldStateFactory { get; }
		public GameTickModuleRegistry GameTickModuleRegistry { get; }
		public GameTickEngine TickEngine { get; }

		public TestGame() {
			LoggerFactory = new Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory();
			WorldStateFactory = new TestWorldStateFactory();
			World = WorldStateFactory.CreateDevWorldState().ToMutable();
			GameDef = new TestGameDefFactory().CreateGameDef();
			PlayerRepository = new PlayerRepository(World);
			PlayerRepositoryWrite = new PlayerRepositoryWrite(World);
			ResourceRepository = new ResourceRepository(World);
			ResourceRepositoryWrite = new ResourceRepositoryWrite(World, ResourceRepository);
			ActionQueueRepository = new ActionQueueRepository(World);
			AssetRepository = new AssetRepository(World, PlayerRepository, ActionQueueRepository);
			AssetRepositoryWrite = new AssetRepositoryWrite(LoggerFactory.CreateLogger<AssetRepositoryWrite>(), World, AssetRepository, ResourceRepository, ResourceRepositoryWrite, ActionQueueRepository, GameDef);
			UnitRepository = new UnitRepository(World, PlayerRepository, AssetRepository);
			UnitRepositoryWrite = new UnitRepositoryWrite(World, GameDef, UnitRepository, ResourceRepositoryWrite);

			var services = new ServiceCollection();
			services.AddSingleton<IGameTickModule>(new ActionQueueExecutor(AssetRepositoryWrite));
			services.AddSingleton<IGameTickModule>(new ResourceGrowthSco(LoggerFactory.CreateLogger<ResourceGrowthSco>(), GameDef, ResourceRepository, ResourceRepositoryWrite, AssetRepository, UnitRepository));
			GameTickModuleRegistry = new GameTickModuleRegistry(LoggerFactory.CreateLogger<GameTickModuleRegistry>(), services.BuildServiceProvider(), GameDef);
			TickEngine = new GameTickEngine(LoggerFactory.CreateLogger<GameTickEngine>(), World, GameDef, GameTickModuleRegistry, PlayerRepositoryWrite);
		}
	}
}
