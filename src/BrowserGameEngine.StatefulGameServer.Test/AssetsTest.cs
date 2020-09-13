using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameDefinition.SCO;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using BrowserGameEngine.StatefulGameServer.GameTicks;
using BrowserGameEngine.StatefulGameServer.GameTicks.Modules;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace BrowserGameEngine.StatefulGameServer.Test {
	internal class TestGame {
		public NullLoggerFactory LoggerFactory { get; private set; }
		public WorldState World { get; private set; }
		public GameDef GameDef { get; private set; }
		public PlayerRepository PlayerRepository { get; private set; }
		public PlayerRepositoryWrite PlayerRepositoryWrite { get; private set; }
		public ResourceRepository ResourceRepository { get; private set; }
		public ResourceRepositoryWrite ResourceRepositoryWrite { get; private set; }
		public AssetRepository AssetRepository { get; private set; }
		public ActionQueueRepository ActionQueueRepository { get; private set; }
		public AssetRepositoryWrite AssetRepositoryWrite { get; private set; }
		public UnitRepository UnitRepository { get; private set; }
		public TestWorldStateFactory WorldStateFactory { get; private set; }
		public GameTickModuleRegistry GameTickModuleRegistry { get; private set; }
		public GameTickEngine TickEngine { get; private set; }

		public TestGame() {
			LoggerFactory = new Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory();
			WorldStateFactory = new TestWorldStateFactory();
			World = WorldStateFactory.CreateDevWorldState().ToMutable();
			GameDef = new TestGameDefFactory().CreateGameDef();
			PlayerRepository = new PlayerRepository(World);
			PlayerRepositoryWrite = new PlayerRepositoryWrite(World);
			ResourceRepository = new ResourceRepository(World);
			ResourceRepositoryWrite = new ResourceRepositoryWrite(World, ResourceRepository);
			AssetRepository = new AssetRepository(World, PlayerRepository);
			ActionQueueRepository = new ActionQueueRepository(World);
			AssetRepositoryWrite = new AssetRepositoryWrite(World, AssetRepository, ResourceRepository, ResourceRepositoryWrite, ActionQueueRepository, GameDef);
			UnitRepository = new UnitRepository(World);

			var services = new ServiceCollection();
			services.AddSingleton<IGameTickModule>(new ActionQueueExecutor(AssetRepositoryWrite));
			services.AddSingleton<IGameTickModule>(new ResourceGrowthSco(LoggerFactory.CreateLogger<ResourceGrowthSco>(), GameDef, ResourceRepository, ResourceRepositoryWrite, AssetRepository, UnitRepository));
			GameTickModuleRegistry = new GameTickModuleRegistry(LoggerFactory.CreateLogger<GameTickModuleRegistry>(), services.BuildServiceProvider(), GameDef);
			TickEngine = new GameTickEngine(LoggerFactory.CreateLogger<GameTickEngine>(), World, GameDef, GameTickModuleRegistry, PlayerRepositoryWrite);
		}
	}

	public class AssetsTest {
		[Fact]
		public void HasAsset() {
			var g = new TestGame();

			Assert.True(g.AssetRepository.HasAsset(g.WorldStateFactory.Player1, Id.AssetDef("asset1")));
			Assert.False(g.AssetRepository.HasAsset(g.WorldStateFactory.Player1, Id.AssetDef("asset2")));
			Assert.False(g.AssetRepository.HasAsset(g.WorldStateFactory.Player1, Id.AssetDef("asset3")));
			Assert.False(g.AssetRepository.HasAsset(g.WorldStateFactory.Player1, Id.AssetDef("asset99")));
			Assert.Equal(1, g.AssetRepository.Get(g.WorldStateFactory.Player1).Count());

			Assert.True(g.AssetRepository.PrerequisitesMet(g.WorldStateFactory.Player1, g.GameDef.GetAssetDef(Id.AssetDef("asset1"))));
			Assert.True(g.AssetRepository.PrerequisitesMet(g.WorldStateFactory.Player1, g.GameDef.GetAssetDef(Id.AssetDef("asset2"))));
			Assert.False(g.AssetRepository.PrerequisitesMet(g.WorldStateFactory.Player1, g.GameDef.GetAssetDef(Id.AssetDef("asset3"))));
		}

		[Fact]
		public void BuildAsset() {
			var g = new TestGame();

			Assert.True(g.AssetRepository.HasAsset(g.WorldStateFactory.Player1, Id.AssetDef("asset1")));
			Assert.False(g.AssetRepository.HasAsset(g.WorldStateFactory.Player1, Id.AssetDef("asset2")));

			g.ResourceRepositoryWrite.AddResources(g.WorldStateFactory.Player1, Id.ResDef("res1"), 1000);
			g.ResourceRepositoryWrite.AddResources(g.WorldStateFactory.Player1, Id.ResDef("res2"), 1000);
			g.AssetRepositoryWrite.BuildAsset(new Commands.BuildAssetCommand(g.WorldStateFactory.Player1, Id.AssetDef("asset2")));

			Assert.False(g.AssetRepository.HasAsset(g.WorldStateFactory.Player1, Id.AssetDef("asset2")));

			g.TickEngine.IncrementWorldTick(9);
			g.TickEngine.CheckAllTicks();
			Assert.False(g.AssetRepository.HasAsset(g.WorldStateFactory.Player1, Id.AssetDef("asset2")));

			g.TickEngine.IncrementWorldTick(1);
			g.TickEngine.CheckAllTicks();
			Assert.True(g.AssetRepository.HasAsset(g.WorldStateFactory.Player1, Id.AssetDef("asset2")));
		}

		[Fact]
		public void BuildAssetNoPrerequisites() {
			var g = new TestGame();

			Assert.True(g.AssetRepository.HasAsset(g.WorldStateFactory.Player1, Id.AssetDef("asset1")));
			Assert.False(g.AssetRepository.HasAsset(g.WorldStateFactory.Player1, Id.AssetDef("asset2")));

			g.ResourceRepositoryWrite.AddResources(g.WorldStateFactory.Player1, Id.ResDef("res1"), 1000);
			g.ResourceRepositoryWrite.AddResources(g.WorldStateFactory.Player1, Id.ResDef("res2"), 1000);
			Assert.Throws<PrerequisitesNotMetException>(() => g.AssetRepositoryWrite.BuildAsset(new Commands.BuildAssetCommand(g.WorldStateFactory.Player1, Id.AssetDef("asset3"))));
		}

		[Fact]
		public void BuildAssetCannotAfford() {
			var g = new TestGame();

			Assert.True(g.AssetRepository.HasAsset(g.WorldStateFactory.Player1, Id.AssetDef("asset1")));
			Assert.False(g.AssetRepository.HasAsset(g.WorldStateFactory.Player1, Id.AssetDef("asset2")));

			g.ResourceRepositoryWrite.AddResources(g.WorldStateFactory.Player1, Id.ResDef("res1"), 1);
			g.ResourceRepositoryWrite.AddResources(g.WorldStateFactory.Player1, Id.ResDef("res2"), 1);
			Assert.Throws<CannotAffordException>(() => g.AssetRepositoryWrite.BuildAsset(new Commands.BuildAssetCommand(g.WorldStateFactory.Player1, Id.AssetDef("asset2"))));
		}
	}
}
