using System;
using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameDefinition.SCO;
using BrowserGameEngine.StatefulGameServer.ActionFeed;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using BrowserGameEngine.StatefulGameServer.GameTicks;
using BrowserGameEngine.StatefulGameServer.GameTicks.Modules;
using BrowserGameEngine.StatefulGameServer.Events;
using BrowserGameEngine.StatefulGameServer.Notifications;
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
		public AllianceInviteRepository AllianceInviteRepository { get; }
		public AllianceInviteRepositoryWrite AllianceInviteRepositoryWrite { get; }
		public AllianceWarRepository AllianceWarRepository { get; }
		public AllianceWarRepositoryWrite AllianceWarRepositoryWrite { get; }
		public AllianceElectionRepository AllianceElectionRepository { get; }
		public AllianceElectionRepositoryWrite AllianceElectionRepositoryWrite { get; }
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
		public TechRepository TechRepository { get; }
		public TechRepositoryWrite TechRepositoryWrite { get; }
		public UnitRepositoryWrite UnitRepositoryWrite { get; }
		public MessageRepository MessageRepository { get; }
		public MessageRepositoryWrite MessageRepositoryWrite { get; }
		public BuildQueueRepository BuildQueueRepository { get; }
		public BuildQueueRepositoryWrite BuildQueueRepositoryWrite { get; }
		public MarketRepository MarketRepository { get; }
		public ResourceHistoryRepository ResourceHistoryRepository { get; }
		public ResourceHistoryRepositoryWrite ResourceHistoryRepositoryWrite { get; }
		public BattleReportRepository BattleReportRepository { get; }
		public BattleReportRepositoryWrite BattleReportRepositoryWrite { get; }
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
			AllianceInviteRepository = new AllianceInviteRepository(Accessor);
			AllianceInviteRepositoryWrite = new AllianceInviteRepositoryWrite(Accessor, AllianceInviteRepository);
			AllianceWarRepository = new AllianceWarRepository(Accessor);
			AllianceWarRepositoryWrite = new AllianceWarRepositoryWrite(Accessor);
			AllianceElectionRepository = new AllianceElectionRepository(Accessor);
			AllianceElectionRepositoryWrite = new AllianceElectionRepositoryWrite(Accessor, AllianceElectionRepository, AllianceRepository);
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
			TechRepository = new TechRepository(Accessor, GameDef);
			TechRepositoryWrite = new TechRepositoryWrite(Accessor, GameDef, TechRepository, ResourceRepository, ResourceRepositoryWrite);
			UnitRepositoryWrite = new UnitRepositoryWrite(LoggerFactory.CreateLogger<UnitRepositoryWrite>(), Accessor, GameDef, UnitRepository, ResourceRepositoryWrite, ResourceRepository, PlayerRepository, PlayerRepositoryWrite, BattleBehavior, UpgradeRepository, TechRepository);
			MessageRepository = new MessageRepository(Accessor);
			MessageRepositoryWrite = new MessageRepositoryWrite(Accessor, TimeProvider.System, NullNotificationService.Instance);
			BuildQueueRepository = new BuildQueueRepository(Accessor);
			BuildQueueRepositoryWrite = new BuildQueueRepositoryWrite(LoggerFactory.CreateLogger<BuildQueueRepositoryWrite>(), Accessor, BuildQueueRepository, AssetRepository, AssetRepositoryWrite, UnitRepository, UnitRepositoryWrite, ResourceRepository, GameDef);
			MarketRepository = new MarketRepository(Accessor, ResourceRepository, ResourceRepositoryWrite);
			ResourceHistoryRepository = new ResourceHistoryRepository(Accessor);
			ResourceHistoryRepositoryWrite = new ResourceHistoryRepositoryWrite(Accessor);
			BattleReportRepository = new BattleReportRepository(Accessor);
			BattleReportRepositoryWrite = new BattleReportRepositoryWrite(Accessor);

			var services = new ServiceCollection();
			services.AddSingleton<IGameTickModule>(new ActionQueueExecutor(AssetRepositoryWrite));
			services.AddSingleton<IGameTickModule>(new ResourceGrowthSco(LoggerFactory.CreateLogger<ResourceGrowthSco>(), GameDef, ResourceRepository, ResourceRepositoryWrite, PlayerRepository, PlayerRepositoryWrite, UnitRepository, UnitRepositoryWrite, new ActionLogger(), TechRepository));
			services.AddSingleton<IGameTickModule>(new ResourceHistoryModule(ResourceRepository, ResourceHistoryRepositoryWrite, Accessor));
			GameTickModuleRegistry = new GameTickModuleRegistry(LoggerFactory.CreateLogger<GameTickModuleRegistry>(), services.BuildServiceProvider(), GameDef);
			TickEngine = new GameTickEngine(LoggerFactory.CreateLogger<GameTickEngine>(), Accessor, GameDef, GameTickModuleRegistry, PlayerRepositoryWrite, TimeProvider.System, NullGameEventPublisher.Instance);
		}
	}
}
