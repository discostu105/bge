using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameDefinition.SCO;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using BrowserGameEngine.StatefulGameServer.GameTicks;
using BrowserGameEngine.StatefulGameServer.GameTicks.Modules;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace BrowserGameEngine.StatefulGameServer.Test {
	public class AssetsTest {
		[Fact]
		public void HasAsset() {
			var factory = new TestWorldStateFactory();
			var world = factory.CreateDevWorldState().ToMutable();
			var gameDef = new TestGameDefFactory().CreateGameDef();
			var playerRepostiroy = new PlayerRepository(world);
			var assetRepository = new AssetRepository(world, playerRepostiroy);

			Assert.True(assetRepository.HasAsset(factory.Player1, Id.AssetDef("asset1")));
			Assert.False(assetRepository.HasAsset(factory.Player1, Id.AssetDef("asset2")));
			Assert.False(assetRepository.HasAsset(factory.Player1, Id.AssetDef("asset3")));
			Assert.False(assetRepository.HasAsset(factory.Player1, Id.AssetDef("asset99")));
			Assert.Equal(1, assetRepository.Get(factory.Player1).Count());

			Assert.True(assetRepository.PrerequisitesMet(factory.Player1, gameDef.GetAssetDef(Id.AssetDef("asset1"))));
			Assert.True(assetRepository.PrerequisitesMet(factory.Player1, gameDef.GetAssetDef(Id.AssetDef("asset2"))));
			Assert.False(assetRepository.PrerequisitesMet(factory.Player1, gameDef.GetAssetDef(Id.AssetDef("asset3"))));
		}

		[Fact]
		public void BuildAsset() {
			var loggerFactory = new Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory();
			var factory = new TestWorldStateFactory();
			var world = factory.CreateDevWorldState().ToMutable();
			var gameDef = new TestGameDefFactory().CreateGameDef();
			var playerRepository = new PlayerRepository(world);
			var playerRepositoryWrite = new PlayerRepositoryWrite(world);
			var resourceRepository = new ResourceRepository(world);
			var resourceRepositoryWrite = new ResourceRepositoryWrite(world, resourceRepository);
			var assetRepository = new AssetRepository(world, playerRepository);
			var assetRepositoryWrite = new AssetRepositoryWrite(world, assetRepository, resourceRepository, resourceRepositoryWrite, gameDef);
			var unitRepository = new UnitRepository(world);
			var services = new ServiceCollection();
			services.AddSingleton<IGameTickModule>(new ResourceGrowthSco1(loggerFactory.CreateLogger<ResourceGrowthSco1>(), gameDef, resourceRepository, resourceRepositoryWrite, assetRepository, unitRepository));
			var gameTickModuleRegistry = new GameTickModuleRegistry(loggerFactory.CreateLogger<GameTickModuleRegistry>(), services.BuildServiceProvider(), gameDef);
			var tickEngine = new GameTickEngine(loggerFactory.CreateLogger<GameTickEngine>(), world, gameDef, gameTickModuleRegistry, playerRepositoryWrite);

			Assert.True(assetRepository.HasAsset(factory.Player1, Id.AssetDef("asset1")));
			Assert.False(assetRepository.HasAsset(factory.Player1, Id.AssetDef("asset2")));

			resourceRepositoryWrite.AddResources(factory.Player1, Id.ResDef("res1"), 1000);
			resourceRepositoryWrite.AddResources(factory.Player1, Id.ResDef("res2"), 1000);
			assetRepositoryWrite.BuildAsset(new Commands.BuildAssetCommand(factory.Player1, Id.AssetDef("asset2")));

			Assert.True(assetRepository.HasAsset(factory.Player1, Id.AssetDef("asset1")));
			Assert.False(assetRepository.HasAsset(factory.Player1, Id.AssetDef("asset2")));

			tickEngine.IncrementWorldTick(30); // should be finished after 30 ticks
			tickEngine.CheckAllTicks();

			Assert.True(assetRepository.HasAsset(factory.Player1, Id.AssetDef("asset1")));
			Assert.True(assetRepository.HasAsset(factory.Player1, Id.AssetDef("asset2")));

		}
	}
}
