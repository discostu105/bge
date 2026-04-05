using BenchmarkDotNet.Running;
using BrowserGameEngine.StatefulGameServer.Benchmarks;
using System;
using System.Diagnostics;

namespace BrowserGameEngine.StatefulGameServer.Benchmarks;

public class Program {
	public static void Main(string[] args) {
		if (args.Length > 0 && args[0] == "--direct") {
			RunDirectTimings();
		} else {
			BenchmarkRunner.Run(typeof(Program).Assembly);
		}
	}

	/// <summary>
	/// Quick manual timing run — does NOT use BenchmarkDotNet warmup/stats machinery.
	/// Used to get fast approximate numbers for the BGE-628 findings document.
	/// </summary>
	private static void RunDirectTimings() {
		Console.WriteLine("=== BGE-628 Game Tick Engine — Direct Timing Run ===");
		Console.WriteLine("(Not a full BDN benchmark — approximate numbers only)");
		Console.WriteLine();

		MeasureScaling();
		MeasureConcurrentReads();
	}

	private static void MeasureScaling() {
		Console.WriteLine("--- Tick Scaling by Player Count ---");
		foreach (int playerCount in new[] { 1, 50, 100, 200, 1000 }) {
			var b = new GameTickScalingBenchmarks { PlayerCount = playerCount };
			b.Setup();

			// Warmup
			for (int i = 0; i < 5; i++) b.SingleWorldTick();

			const int iterations = 500;
			var sw = Stopwatch.StartNew();
			for (int i = 0; i < iterations; i++) b.SingleWorldTick();
			sw.Stop();

			double avgUs = sw.Elapsed.TotalMicroseconds / iterations;
			Console.WriteLine($"  Players={playerCount,5} | SingleWorldTick avg = {avgUs,8:F1} µs");
		}
		Console.WriteLine();

		Console.WriteLine("--- ResourceGrowth Module Isolation ---");
		foreach (int playerCount in new[] { 50, 100, 200 }) {
			var b = new ResourceGrowthModuleBenchmarks { PlayerCount = playerCount };
			b.Setup();

			// Warmup
			for (int i = 0; i < 3; i++) b.ResourceGrowthAllPlayers();

			const int iterations = 200;
			var sw = Stopwatch.StartNew();
			for (int i = 0; i < iterations; i++) b.ResourceGrowthAllPlayers();
			sw.Stop();

			double avgUs = sw.Elapsed.TotalMicroseconds / iterations;
			Console.WriteLine($"  Players={playerCount,5} | ResourceGrowthAllPlayers avg = {avgUs,8:F1} µs");
		}
		Console.WriteLine();
	}

	private static void MeasureConcurrentReads() {
		Console.WriteLine("--- Concurrent Reads During Tick (100 players) ---");
		var b = new ConcurrentReadDuringTickBenchmarks();
		b.Setup();

		// Warmup
		for (int i = 0; i < 5; i++) b.ConcurrentReadsDuringTick();

		const int iterations = 100;
		var sw = Stopwatch.StartNew();
		for (int i = 0; i < iterations; i++) b.ConcurrentReadsDuringTick();
		sw.Stop();

		double avgMs = sw.Elapsed.TotalMilliseconds / iterations;
		Console.WriteLine($"  ConcurrentReadsDuringTick avg = {avgMs:F2} ms (no deadlock = PASS)");
		Console.WriteLine();
	}
}
