using BrowserGameEngine.GameDefinition;

namespace BrowserGameEngine.BalanceSim.Simulations;

public static class ResourceSimulation
{
	// Constants matching ResourceGrowthSco
	private const decimal BaseIncomeMinerals = 10m;
	private const decimal BaseIncomeGas = 10m;
	private const decimal MineralsPerWorker = 4m;
	private const decimal GasPerWorker = 4m;
	private const decimal MineralEfficiencyFactor = 0.03m;
	private const decimal GasEfficiencyFactor = 0.06m;
	private const decimal EfficiencyMin = 0.2m;
	private const decimal EfficiencyMax = 100m;

	public static void Run(GameDef gameDef, Dictionary<string, string> options) {
		gameDef = SimulationHelpers.ApplyOverrides(gameDef, options);

		int mineralWorkers = options.GetInt("mineral-workers", 10);
		int gasWorkers = options.GetInt("gas-workers", 5);
		decimal land = options.GetDecimal("land", 50);
		int ticks = options.GetInt("ticks", 100);
		bool csv = options.GetBool("csv");

		var results = Simulate(mineralWorkers, gasWorkers, land, ticks);

		if (csv) {
			PrintCsv(results);
		} else {
			PrintMarkdown(results, mineralWorkers, gasWorkers, land);
		}
	}

	public static List<TickSnapshot> Simulate(int mineralWorkers, int gasWorkers, decimal land, int ticks) {
		var snapshots = new List<TickSnapshot>();
		decimal totalMinerals = 0;
		decimal totalGas = 0;

		for (int tick = 0; tick <= ticks; tick++) {
			if (tick > 0) {
				decimal mineralIncome = CalculateWorkerIncome(mineralWorkers, land, MineralsPerWorker, MineralEfficiencyFactor) + BaseIncomeMinerals;
				decimal gasIncome = CalculateWorkerIncome(gasWorkers, land, GasPerWorker, GasEfficiencyFactor) + BaseIncomeGas;
				totalMinerals += mineralIncome;
				totalGas += gasIncome;

				snapshots.Add(new TickSnapshot(tick, land, mineralWorkers, gasWorkers, mineralIncome, gasIncome, totalMinerals, totalGas));
			} else {
				snapshots.Add(new TickSnapshot(0, land, mineralWorkers, gasWorkers, 0, 0, 0, 0));
			}
		}

		return snapshots;
	}

	private static decimal CalculateWorkerIncome(int workers, decimal land, decimal perWorker, decimal efficiencyFactor) {
		if (workers == 0) return 0m;
		decimal efficiency = Math.Clamp(land / (workers * efficiencyFactor), EfficiencyMin, EfficiencyMax);
		return workers * perWorker * efficiency / 100m;
	}

	private static void PrintMarkdown(List<TickSnapshot> snapshots, int mineralWorkers, int gasWorkers, decimal land) {
		Console.WriteLine($"## Resource Simulation");
		Console.WriteLine($"Mineral workers: {mineralWorkers}, Gas workers: {gasWorkers}, Land: {land}");
		Console.WriteLine();
		Console.WriteLine("| Tick | Land | MW | GW | M/tick | G/tick | Total M | Total G |");
		Console.WriteLine("|-----:|-----:|---:|---:|-------:|-------:|--------:|--------:|");
		foreach (var s in snapshots) {
			Console.WriteLine($"| {s.Tick,4} | {s.Land,4:F0} | {s.MineralWorkers,2} | {s.GasWorkers,2} | {s.MineralIncome,6:F1} | {s.GasIncome,6:F1} | {s.TotalMinerals,7:F0} | {s.TotalGas,7:F0} |");
		}
	}

	private static void PrintCsv(List<TickSnapshot> snapshots) {
		Console.WriteLine("Tick,Land,MineralWorkers,GasWorkers,MineralIncome,GasIncome,TotalMinerals,TotalGas");
		foreach (var s in snapshots) {
			Console.WriteLine($"{s.Tick},{s.Land:F0},{s.MineralWorkers},{s.GasWorkers},{s.MineralIncome:F2},{s.GasIncome:F2},{s.TotalMinerals:F2},{s.TotalGas:F2}");
		}
	}

	public record TickSnapshot(int Tick, decimal Land, int MineralWorkers, int GasWorkers, decimal MineralIncome, decimal GasIncome, decimal TotalMinerals, decimal TotalGas);
}
