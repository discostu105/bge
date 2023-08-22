using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using BenchmarkDotNet.Jobs;
using BrowserGameEngine.StatefulGameServer.Test;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BrowserGameEngine.StatefulGameServer.Benchmarks {

    [ShortRunJob]
    [MemoryDiagnoser]
    public class GameTickEngineBenchmarks {
        private TestGame singlePlayerGame;
        private TestGame thousandPlayerGame;

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
}
