using BrowserGameEngine.Persistence;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.ActionFeed;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using BrowserGameEngine.StatefulGameServer.GameRegistry;
using BrowserGameEngine.StatefulGameServer.GameTicks;
using BrowserGameEngine.StatefulGameServer.GameTicks.Modules;
using BrowserGameEngine.StatefulGameServer.Events;
using BrowserGameEngine.StatefulGameServer.Notifications;
using BrowserGameEngine.StatefulGameServer.Repositories;
using BrowserGameEngine.StatefulGameServer.Repositories.Chat;
using BrowserGameEngine.StatefulGameServer.Repositories.Tournament;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;

namespace BrowserGameEngine.StatefulGameServer {
	public static class GameServerExtensions {
		public static void AddGameServer(this IServiceCollection services, IBlobStorage storage, BrowserGameEngine.StatefulGameServer.GameRegistry.GameRegistry gameRegistry, IWorldStateFactory worldStateFactory) {
			var defaultInstance = gameRegistry.GetDefaultInstance();
			services.AddSingleton(gameRegistry);
			services.AddSingleton(gameRegistry.GlobalState);
			services.AddSingleton<IWorldStateAccessor>(defaultInstance.WorldStateAccessor);
			services.AddSingleton(defaultInstance.WorldState);
			services.AddSingleton(defaultInstance.GameDef);
			services.AddSingleton(TimeProvider.System);
			services.AddSingleton<IGameEventPublisher, NullGameEventPublisher>();
		services.AddSingleton<ISpectatorEventPublisher, NullSpectatorEventPublisher>();
			services.AddSingleton<GameRepository>();

			services.AddSingleton<UserRepository>();
			services.AddSingleton<UserRepositoryWrite>();
			services.AddSingleton<PlayerRepository>();
			services.AddSingleton<PlayerRepositoryWrite>();
			services.AddSingleton<OnlineStatusRepository>();
			services.AddSingleton<ResourceRepository>();
			services.AddSingleton<ResourceRepositoryWrite>();
			services.AddSingleton<AllianceRepository>();
			services.AddSingleton<AllianceRepositoryWrite>();
			services.AddSingleton<AllianceChatRepository>();
			services.AddSingleton<AllianceChatRepositoryWrite>();
			services.AddSingleton<AllianceInviteRepository>();
			services.AddSingleton<AllianceInviteRepositoryWrite>();
			services.AddSingleton<AllianceWarRepository>();
			services.AddSingleton<AllianceWarRepositoryWrite>();
			services.AddSingleton<AllianceElectionRepository>();
			services.AddSingleton<AllianceElectionRepositoryWrite>();
			services.AddSingleton<ChatRepositoryWrite>();
			services.AddSingleton<AssetRepository>();
			services.AddSingleton<AssetRepositoryWrite>();
			services.AddSingleton<UnitRepository>();
			services.AddSingleton<UnitRepositoryWrite>();
			services.AddSingleton<ColonizeRepositoryWrite>();
			services.AddSingleton<ActionQueueRepository>();
			services.AddSingleton<MessageRepository>();
			services.AddSingleton<MessageRepositoryWrite>();
			services.AddSingleton<BattleReportGenerator>();
			services.AddSingleton<BattleReportRepository>();
			services.AddSingleton<BattleReportRepositoryWrite>();
			services.AddSingleton<UpgradeRepository>();
			services.AddSingleton<UpgradeRepositoryWrite>();
			services.AddSingleton<BuildQueueRepository>();
			services.AddSingleton<BuildQueueRepositoryWrite>();
			services.AddSingleton<MarketRepository>();
			services.AddSingleton<TradeRepository>();
			services.AddSingleton<TradeRepositoryWrite>();
			services.AddSingleton<ResourceHistoryRepository>();
			services.AddSingleton<ResourceHistoryRepositoryWrite>();
			services.AddSingleton<GameReplayRepository>();
			services.AddSingleton<TournamentRepository>();
			services.AddSingleton<TournamentRepositoryWrite>();
			services.AddSingleton<TournamentEngine>();
			services.AddSingleton<AdminAuditLog>();
			services.AddSingleton<ReportStore>();

			services.AddSingleton<IActionLogger, ActionLogger>();
			services.AddSingleton<IGameTickModule, ActionQueueExecutor>();
			services.AddSingleton<IGameTickModule, UnitReturn>();
			services.AddSingleton<IGameTickModule, ResourceGrowthSco>();
			services.AddSingleton<IGameTickModule, NewPlayerProtectionModule>();
			services.AddSingleton<IGameTickModule, UpgradeTimer>();
			services.AddSingleton<IGameTickModule, BuildQueueModule>();
			services.AddSingleton<IGameTickModule, ResourceHistoryModule>();
			services.AddSingleton<IGameTickModule, ElectionTickModule>();
			services.AddSingleton<IGameTickModule, GameFinalizationModule>();
			services.AddSingleton<IGameTickModule, SpectatorTickModule>();
			services.AddSingleton<GameTickModuleRegistry>(); // Modules need to be registered before this
			services.AddSingleton<GameTickEngine>();

			services.AddSingleton<IBattleBehavior, BattleBehaviorScoOriginal>(); // TODO: make this configurable through GameDef

			var serializer = new GameStateJsonSerializer();
			var persistenceService = new PersistenceService(storage, serializer);
			var globalSerializer = new GlobalStateJsonSerializer();
			var globalPersistenceService = new GlobalPersistenceService(storage, globalSerializer);
			services.AddSingleton<IBlobStorage>(storage);
			services.AddSingleton(serializer);
			services.AddSingleton(persistenceService);
			services.AddSingleton(globalSerializer);
			services.AddSingleton(globalPersistenceService);
			services.AddSingleton<IWorldStateFactory>(worldStateFactory);
			services.AddSingleton<IGameNotificationService, NullGameNotificationService>();
			services.AddSingleton<IPlayerNotificationService, InMemoryPlayerNotificationService>();
			services.AddSingleton<INotificationService, NotificationService>();
			services.AddSingleton<GameLifecycleEngine>();
		}
	}
}
