using BrowserGameEngine.Persistence;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.ActionFeed;
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
			services.AddSingleton(TimeProvider.System);
			services.AddSingleton<GameRepository>();
			services.AddSingleton<UserRepository>();
			services.AddSingleton<UserRepositoryWrite>();
			services.AddSingleton<PlayerRepository>();
			services.AddSingleton<PlayerRepositoryWrite>();
			services.AddSingleton<OnlineStatusRepository>();
			services.AddSingleton<ResourceRepository>();
			services.AddSingleton<ResourceRepositoryWrite>();
			services.AddSingleton<ScoreRepository>();
			services.AddSingleton<AllianceRepository>();
			services.AddSingleton<AllianceRepositoryWrite>();
			services.AddSingleton<AssetRepository>();
			services.AddSingleton<AssetRepositoryWrite>();
			services.AddSingleton<UnitRepository>();
			services.AddSingleton<UnitRepositoryWrite>();
			services.AddSingleton<ColonizeRepositoryWrite>();
			services.AddSingleton<ActionQueueRepository>();
			services.AddSingleton<MessageRepository>();
			services.AddSingleton<MessageRepositoryWrite>();
			services.AddSingleton<BattleReportGenerator>();
			services.AddSingleton<UpgradeRepository>();
			services.AddSingleton<UpgradeRepositoryWrite>();
			services.AddSingleton<BuildQueueRepository>();
			services.AddSingleton<BuildQueueRepositoryWrite>();

			services.AddSingleton<IActionLogger, ActionLogger>();
			services.AddSingleton<IGameTickModule, ActionQueueExecutor>();
			services.AddSingleton<IGameTickModule, UnitReturn>();
			services.AddSingleton<IGameTickModule, ResourceGrowthSco>();
			services.AddSingleton<IGameTickModule, NewPlayerProtectionModule>();
			services.AddSingleton<IGameTickModule, UpgradeTimer>();
			services.AddSingleton<IGameTickModule, BuildQueueModule>();
			services.AddSingleton<GameTickModuleRegistry>(); // Modules need to be registered before this
			services.AddSingleton<GameTickEngine>();

			services.AddSingleton<IBattleBehavior, BattleBehaviorScoOriginal>(); // TODO: make this configurable through GameDef
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
