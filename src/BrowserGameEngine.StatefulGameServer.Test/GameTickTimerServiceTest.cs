using BrowserGameEngine.FrontendServer;
using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameDefinition.SCO;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.ActionFeed;
using BrowserGameEngine.StatefulGameServer.Commands;
using BrowserGameEngine.StatefulGameServer.Events;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using BrowserGameEngine.StatefulGameServer.GameTicks;
using BrowserGameEngine.StatefulGameServer.GameTicks.Modules;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using GameRegistryNs = BrowserGameEngine.StatefulGameServer.GameRegistry;

namespace BrowserGameEngine.StatefulGameServer.Test {
	/// <summary>
	/// Regression coverage for the production bug where build queues on
	/// non-default games stayed stuck (e.g. "Building · ready in 20t" forever
	/// the day after the order was placed). Two layered defects caused it:
	///
	///   1) Program.cs only called <c>SetTickEngine</c> once at startup, so any
	///      <see cref="GameRegistryNs.GameInstance"/> created at runtime by
	///      <c>GamesController.Create</c> or <c>TournamentEngine</c> had a null
	///      <c>TickEngine</c> and was silently skipped by the timer service.
	///   2) Even with a wired engine, the singleton tick modules read their
	///      <see cref="WorldState"/> through <see cref="HttpContextWorldStateAccessor"/>,
	///      which falls back to <c>defaultWorldState</c> when no HttpContext
	///      exists — so every iteration of the timer loop ticked the same
	///      default game regardless of which instance was being processed.
	///
	/// The structural fix removes <c>GameInstance.TickEngine</c> entirely (no
	/// field that can be forgotten) and makes <see cref="GameTickTimerService"/>
	/// push each instance's <see cref="WorldState"/> as the ambient override on
	/// the shared accessor before invoking the singleton engine. These tests
	/// pin both behaviours.
	/// </summary>
	public class GameTickTimerServiceTest {
		private static readonly GameDef TestGameDef = new TestGameDefFactory().CreateGameDef();

		// Builds a self-contained tick stack — one accessor, one engine, one
		// set of modules — that two GameInstances share, mirroring the
		// production singleton DI graph that the bug surfaced under.
		private sealed class SharedTickStack {
			public HttpContextWorldStateAccessor Accessor { get; }
			public GameTickEngine Engine { get; }
			public GameRegistryNs.GameRegistry Registry { get; }

			public SharedTickStack(WorldState defaultWorld, GameRegistryNs.GameRegistry registry) {
				Registry = registry;
				// No HttpContext available — mirrors the timer service's runtime context.
				var httpContextAccessor = new HttpContextAccessor();
				Accessor = new HttpContextWorldStateAccessor(httpContextAccessor, registry, defaultWorld);

				// Construct modules and repositories against the SAME accessor
				// the production DI uses, so this test catches regressions where
				// modules bypass the ambient world-state push.
				var resourceRepo = new ResourceRepository(Accessor, TestGameDef);
				var resourceRepoWrite = new ResourceRepositoryWrite(Accessor, resourceRepo, TestGameDef);
				var playerRepo = new PlayerRepository(Accessor, resourceRepo, new AllianceRepository(Accessor));
				var actionQueueRepo = new ActionQueueRepository(Accessor);
				var assetRepo = new AssetRepository(Accessor, playerRepo, actionQueueRepo);
				var assetRepoWrite = new AssetRepositoryWrite(
					NullLogger<AssetRepositoryWrite>.Instance, Accessor, assetRepo,
					resourceRepo, resourceRepoWrite, actionQueueRepo, TestGameDef);
				var unitRepo = new UnitRepository(Accessor, TestGameDef, playerRepo, assetRepo);
				var unitRepoWrite = new UnitRepositoryWrite(
					NullLogger<UnitRepositoryWrite>.Instance, Accessor, TestGameDef, unitRepo,
					resourceRepoWrite, resourceRepo, playerRepo,
					new BattleBehaviorScoOriginal(NullLogger<IBattleBehavior>.Instance),
					new UpgradeRepository(Accessor));
				var playerRepoWrite = new PlayerRepositoryWrite(Accessor, TimeProvider.System);

				var services = new ServiceCollection();
				services.AddSingleton<IGameTickModule>(new ActionQueueExecutor(assetRepoWrite));
				services.AddSingleton<IGameTickModule>(new ResourceGrowthSco(
					NullLogger<ResourceGrowthSco>.Instance, TestGameDef,
					resourceRepo, resourceRepoWrite, playerRepo, unitRepo, unitRepoWrite,
					new ActionLogger()));
				var moduleRegistry = new GameTickModuleRegistry(
					NullLogger<GameTickModuleRegistry>.Instance,
					services.BuildServiceProvider(),
					TestGameDef);

				Engine = new GameTickEngine(
					NullLogger<GameTickEngine>.Instance, Accessor, TestGameDef,
					moduleRegistry, playerRepoWrite, TimeProvider.System,
					NullGameEventPublisher.Instance);

				BuildAssetWriter = assetRepoWrite;
				AssetReader = assetRepo;
			}

			public AssetRepositoryWrite BuildAssetWriter { get; }
			public AssetRepository AssetReader { get; }
		}

		private static GameRecordImmutable MakeRecord(string gameId) =>
			new GameRecordImmutable(new GameId(gameId), gameId, "sco", GameStatus.Active,
				DateTime.UtcNow, DateTime.UtcNow.AddDays(1), TimeSpan.FromSeconds(10));

		private static GameRegistryNs.GameInstance MakeInstance(string gameId, WorldState world)
			=> new GameRegistryNs.GameInstance(MakeRecord(gameId), world, TestGameDef);

		// Captures Bug #1 + Bug #2 together: a build queued on the non-default
		// game must complete when the timer service drives ticks, even though
		// the singleton engine + accessor are pointed at the default world by
		// default.
		[Fact]
		public void DoWork_TicksNonDefaultGame_AndCompletesQueuedBuild() {
			var factory = new TestWorldStateFactory();
			var game1World = (factory.CreateDevWorldState(1) with { GameId = new GameId("game1") }).ToMutable();
			var game2World = (factory.CreateDevWorldState(1) with { GameId = new GameId("game2") }).ToMutable();

			var registry = new GameRegistryNs.GameRegistry(new GlobalState());
			registry.Register(MakeInstance("game1", game1World)); // default (first registered)
			registry.Register(MakeInstance("game2", game2World));

			// defaultWorld for the accessor mirrors AddGameServer's startup wiring.
			var stack = new SharedTickStack(defaultWorld: game1World, registry);

			// Queue a build on game2 (non-default). Without the fix, the timer
			// service would keep ticking game1's world while game2 stays frozen.
			var player = factory.Player1;
			using (var scope = stack.Accessor.PushAmbient(game2World)) {
				stack.BuildAssetWriter.BuildAsset(new BuildAssetCommand(player, Id.AssetDef("asset2")));
			}
			Assert.False(stack.AssetReader.IsBuiltOnAmbient(player, "asset2", stack.Accessor, game2World));
			Assert.False(stack.AssetReader.IsBuiltOnAmbient(player, "asset2", stack.Accessor, game1World));

			// Drive the timer service. Force ticks via IncrementWorldTick to
			// avoid the wall-clock wait.
			var service = new GameTickTimerService(
				NullLogger<GameTickTimerService>.Instance, registry, stack.Engine, stack.Accessor);
			AdvanceWorldAndTick(stack, game2World, ticks: 10);
			InvokeDoWork(service);

			// Build (BuildTimeTicks=10 in TestGameDef) must now exist on game2,
			// and game1 must be untouched.
			Assert.True(stack.AssetReader.IsBuiltOnAmbient(player, "asset2", stack.Accessor, game2World),
				"Build queued on game2 should complete after the timer service drives ticks.");
			Assert.False(stack.AssetReader.IsBuiltOnAmbient(player, "asset2", stack.Accessor, game1World),
				"game1 must be unaffected — ambient world-state push must isolate per-game state.");
		}

		// Pins Bug #3 (latent): GameLifecycleEngine.PauseTicks used to call
		// PauseTicks on the singleton engine, which would freeze every game on
		// the server. The fix moves pause to per-instance — verify the timer
		// service honors it without affecting siblings.
		[Fact]
		public void DoWork_SkipsPausedInstance_ButTicksOthers() {
			var factory = new TestWorldStateFactory();
			var game1World = (factory.CreateDevWorldState(1) with { GameId = new GameId("game1") }).ToMutable();
			var game2World = (factory.CreateDevWorldState(1) with { GameId = new GameId("game2") }).ToMutable();

			var registry = new GameRegistryNs.GameRegistry(new GlobalState());
			var instance1 = MakeInstance("game1", game1World);
			var instance2 = MakeInstance("game2", game2World);
			registry.Register(instance1);
			registry.Register(instance2);

			var stack = new SharedTickStack(defaultWorld: game1World, registry);

			// Pause game1; queue identical builds on both.
			instance1.Pause();
			var player = factory.Player1;
			using (var scope = stack.Accessor.PushAmbient(game1World))
				stack.BuildAssetWriter.BuildAsset(new BuildAssetCommand(player, Id.AssetDef("asset2")));
			using (var scope = stack.Accessor.PushAmbient(game2World))
				stack.BuildAssetWriter.BuildAsset(new BuildAssetCommand(player, Id.AssetDef("asset2")));

			var service = new GameTickTimerService(
				NullLogger<GameTickTimerService>.Instance, registry, stack.Engine, stack.Accessor);
			AdvanceWorldAndTick(stack, game1World, ticks: 10);
			AdvanceWorldAndTick(stack, game2World, ticks: 10);
			InvokeDoWork(service);

			Assert.False(stack.AssetReader.IsBuiltOnAmbient(player, "asset2", stack.Accessor, game1World),
				"Paused game1 must not advance.");
			Assert.True(stack.AssetReader.IsBuiltOnAmbient(player, "asset2", stack.Accessor, game2World),
				"Unpaused game2 must still tick to completion.");
		}

		// Direct unit test of the ambient-scope mechanism the fix relies on.
		[Fact]
		public void HttpContextWorldStateAccessor_PushAmbient_OverridesDefault_AndPopsOnDispose() {
			var factory = new TestWorldStateFactory();
			var defaultWorld = factory.CreateDevWorldState(0).ToMutable();
			var otherWorld = factory.CreateDevWorldState(0).ToMutable();
			var registry = new GameRegistryNs.GameRegistry(new GlobalState());
			var accessor = new HttpContextWorldStateAccessor(new HttpContextAccessor(), registry, defaultWorld);

			Assert.Same(defaultWorld, accessor.WorldState);
			using (var scope = accessor.PushAmbient(otherWorld)) {
				Assert.Same(otherWorld, accessor.WorldState);
			}
			Assert.Same(defaultWorld, accessor.WorldState);
		}

		// CheckAllTicks is bounded by wall-clock: it loops while IsTickDue
		// (LastUpdate stale) AND there are unprocessed players. Force enough
		// world-tick increments per game so a single DoWork iteration drains
		// the queued action.
		private static void AdvanceWorldAndTick(SharedTickStack stack, WorldState world, int ticks) {
			using var scope = stack.Accessor.PushAmbient(world);
			stack.Engine.IncrementWorldTick(ticks);
		}

		private static void InvokeDoWork(GameTickTimerService service) {
			// DoWork is private; reach it via reflection to avoid making it
			// public solely for testing. The signature matches Timer's TimerCallback.
			var method = typeof(GameTickTimerService).GetMethod("DoWork",
				System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
			method.Invoke(service, new object?[] { null });
		}
	}

	internal static class AssetRepositoryAmbientExtensions {
		// HasAsset goes through the shared accessor — push the world we want
		// to query so the assertion targets that game specifically.
		internal static bool IsBuiltOnAmbient(this AssetRepository repo, PlayerId playerId, string assetId,
				IWorldStateAccessor accessor, WorldState world) {
			using var scope = accessor.PushAmbient(world);
			return repo.HasAsset(playerId, Id.AssetDef(assetId));
		}
	}
}
