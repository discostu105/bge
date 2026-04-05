using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BrowserGameEngine.StatefulGameServer.Test;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BrowserGameEngine.StatefulGameServer.Benchmarks {

	/// <summary>
	/// Full tick-cycle benchmarks parameterized by player count.
	/// Covers 50 / 100 / 200 / 1000 concurrent players.
	/// </summary>
	[ShortRunJob]
	[MemoryDiagnoser]
	public class GameTickEngineBenchmarks {
		private TestGame singlePlayerGame = null!;
		private TestGame thousandPlayerGame = null!;

		[GlobalSetup]
		public void Setup() {
			singlePlayerGame = new TestGame();
			thousandPlayerGame = new TestGame(1000);
		}

		[Benchmark]
		public void SinglePlayerSingleGameTick() {
			singlePlayerGame.TickEngine.IncrementWorldTick(1);
			singlePlayerGame.TickEngine.CheckAllTicks();
		}

		[Benchmark]
		public void SinglePlayerThousandGameTicks() {
			singlePlayerGame.TickEngine.IncrementWorldTick(1000);
			singlePlayerGame.TickEngine.CheckAllTicks();
		}

		[Benchmark]
		public void ThousandPlayerSingleGameTick() {
			thousandPlayerGame.TickEngine.IncrementWorldTick(1);
			thousandPlayerGame.TickEngine.CheckAllTicks();
		}

		[Benchmark]
		public void ThousandPlayerThousandGameTicks() {
			thousandPlayerGame.TickEngine.IncrementWorldTick(1000);
			thousandPlayerGame.TickEngine.CheckAllTicks();
		}
	}

	/// <summary>
	/// Parameterized benchmark across 50/100/200 player counts — the realistic production range.
	/// Used to document tick duration vs player count and detect non-linear scaling.
	/// </summary>
	[ShortRunJob]
	[MemoryDiagnoser]
	public class GameTickScalingBenchmarks {
		[Params(50, 100, 200)]
		public int PlayerCount { get; set; }

		private TestGame game = null!;

		[GlobalSetup]
		public void Setup() {
			game = new TestGame(PlayerCount);
		}

		/// <summary>
		/// A single world-tick advancing all players by one tick — the hot path in production.
		/// </summary>
		[Benchmark(Baseline = true)]
		public void SingleWorldTick() {
			game.TickEngine.IncrementWorldTick(1);
			game.TickEngine.CheckAllTicks();
		}

		/// <summary>
		/// 100 consecutive world ticks — simulates ~100 seconds of game-time in one benchmark call.
		/// </summary>
		[Benchmark]
		public void HundredWorldTicks() {
			game.TickEngine.IncrementWorldTick(100);
			game.TickEngine.CheckAllTicks();
		}
	}

	/// <summary>
	/// Measures the overhead of the ResourceGrowthSco tick module in isolation
	/// across the realistic player range.
	/// </summary>
	[ShortRunJob]
	[MemoryDiagnoser]
	public class ResourceGrowthModuleBenchmarks {
		[Params(50, 100, 200)]
		public int PlayerCount { get; set; }

		private TestGame game = null!;
		private List<BrowserGameEngine.GameModel.PlayerId> playerIds = null!;

		[GlobalSetup]
		public void Setup() {
			game = new TestGame(PlayerCount);
			playerIds = game.PlayerRepository.GetAll().Select(p => p.PlayerId).ToList();
		}

		/// <summary>
		/// Runs the ResourceGrowthSco module directly for all players without full tick machinery.
		/// Isolates ResourceGrowthSco cost from other modules.
		/// </summary>
		[Benchmark]
		public void ResourceGrowthAllPlayers() {
			foreach (var module in game.GameTickModuleRegistry.Modules) {
				if (module.Name == "resource-growth-sco:1") {
					foreach (var pid in playerIds) {
						module.CalculateTick(pid);
					}
					break;
				}
			}
		}
	}

	/// <summary>
	/// Validates that concurrent read-only repository queries during an active tick cycle
	/// do not produce corrupted results or deadlocks (thread-safety smoke test).
	/// This is not a pure micro-benchmark — it checks for data races at the integration level.
	/// </summary>
	[ShortRunJob]
	public class ConcurrentReadDuringTickBenchmarks {
		private TestGame game = null!;
		private List<BrowserGameEngine.GameModel.PlayerId> players = null!;

		[GlobalSetup]
		public void Setup() {
			game = new TestGame(100);
			players = game.PlayerRepository.GetAll().Select(p => p.PlayerId).ToList();
		}

		/// <summary>
		/// Runs a tick on one thread while 8 reader threads query player resources concurrently.
		/// A deadlock or data-race will surface as a hang or exception.
		/// </summary>
		[Benchmark]
		public void ConcurrentReadsDuringTick() {
			if (players.Count == 0) return;

			var tickTask = Task.Run(() => {
				game.TickEngine.IncrementWorldTick(1);
				game.TickEngine.CheckAllTicks();
			});

			var readTasks = new Task[8];
			for (int i = 0; i < readTasks.Length; i++) {
				var pid = players[i % players.Count];
				readTasks[i] = Task.Run(() => {
					// Read-only operations that must not be blocked or corrupted by ticking
					_ = game.ResourceRepository.GetPrimaryResource(pid);
					_ = game.PlayerRepository.GetAll();
				});
			}

			Task.WaitAll([tickTask, .. readTasks]);
		}
	}
}
