using BrowserGameEgnine.Persistence;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using BrowserGameEngine.StatefulGameServer.GameTicks;
using BrowserGameEngine.StatefulGameServer.GameTicks.Modules;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BrowserGameEngine.StatefulGameServer {
	public static class GameServerExtensions {
		public static async Task AddGameServer(this IServiceCollection services, IBlobStorage storage, WorldStateImmutable defaultWorldState) {
			await InitPersistenceAndGameState(services, storage, defaultWorldState);
			services.AddSingleton<PlayerRepository>();
			services.AddSingleton<PlayerRepositoryWrite>();
			services.AddSingleton<ResourceRepository>();
			services.AddSingleton<ResourceRepositoryWrite>();
			services.AddSingleton<ScoreRepository>();
			services.AddSingleton<AssetRepository>();
			services.AddSingleton<AssetRepositoryWrite>();
			services.AddSingleton<UnitRepository>();
			services.AddSingleton<UnitRepositoryWrite>();
			services.AddSingleton<ActionQueueRepository>();

			services.AddSingleton<IGameTickModule, ActionQueueExecutor>();
			services.AddSingleton<IGameTickModule, UnitReturn>();
			services.AddSingleton<IGameTickModule, ResourceGrowthSco>();
			services.AddSingleton<GameTickModuleRegistry>(); // Modules need to be registered before this
			services.AddSingleton<GameTickEngine>();
		}

		private static async Task InitPersistenceAndGameState(IServiceCollection services, IBlobStorage storage, WorldStateImmutable defaultWorldState) {
			var serializer = new GameStateJsonSerializer();
			var persistenceService = new PersistenceService(storage, serializer);
			
			services.AddSingleton<IBlobStorage>(storage);
			services.AddSingleton<GameStateJsonSerializer>(serializer);
			services.AddSingleton<PersistenceService>(persistenceService);

			if (persistenceService.WorldStateExists()) {
				// state exists. use it. ignore default state.
				var worldStateImmutable = await persistenceService.LoadWorldState();
				services.AddSingleton<WorldState>(worldStateImmutable.ToMutable());
			} else {
				// no state stored yet. use default state.
				services.AddSingleton<WorldState>(defaultWorldState.ToMutable());
			}
		}
	}
}
