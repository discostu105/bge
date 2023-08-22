using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnostics.Windows;
using BenchmarkDotNet.Running;
using BrowserGameEngine.StatefulGameServer.Benchmarks;

namespace BrowserGameEngine.StatefulGameServer.Benchmarks;

public class Program {
    public static void Main(string[] args) {
        BenchmarkRunner.Run(typeof(Program).Assembly);
        //RunDirectly();
    }

    private static void RunDirectly() {
        var benchmark = new GameTickEngineBenchmarks();
        benchmark.Setup();
        for (int i = 0; i < 100000; i++) {
            benchmark.SinglePlayerThousandGameTicks();
        }
    }
}
