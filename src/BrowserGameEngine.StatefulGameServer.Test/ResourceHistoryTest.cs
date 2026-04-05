using System;
using System.Linq;
using BrowserGameEngine.GameModel;
using Xunit;

namespace BrowserGameEngine.StatefulGameServer.Test {

	public class ResourceHistoryTest {
		[Fact]
		public void AppendSnapshot_AddsToHistory() {
			var g = new TestGame();

			g.ResourceHistoryRepositoryWrite.AppendSnapshot(g.Player1,
				new ResourceSnapshot(1, DateTime.UtcNow, 100m, 50m, 10m));

			var history = g.ResourceHistoryRepository.GetHistory(g.Player1);
			Assert.Single(history);
			Assert.Equal(1, history[0].Tick);
			Assert.Equal(100m, history[0].Minerals);
			Assert.Equal(50m, history[0].Gas);
			Assert.Equal(10m, history[0].Land);
		}

		[Fact]
		public void AppendSnapshot_MultipleSnapshots_PreservesOrder() {
			var g = new TestGame();

			for (int i = 1; i <= 5; i++) {
				g.ResourceHistoryRepositoryWrite.AppendSnapshot(g.Player1,
					new ResourceSnapshot(i, DateTime.UtcNow, i * 100m, i * 50m, 10m));
			}

			var history = g.ResourceHistoryRepository.GetHistory(g.Player1);
			Assert.Equal(5, history.Count);
			for (int i = 0; i < history.Count; i++) {
				Assert.Equal(i + 1, history[i].Tick);
			}
		}

		[Fact]
		public void AppendSnapshot_TrimsAtMaxSize() {
			var g = new TestGame();

			for (int i = 0; i < 105; i++) {
				g.ResourceHistoryRepositoryWrite.AppendSnapshot(g.Player1,
					new ResourceSnapshot(i, DateTime.UtcNow, 100m, 50m, 10m));
			}

			var history = g.ResourceHistoryRepository.GetHistory(g.Player1);
			Assert.Equal(100, history.Count);
			Assert.Equal(5, history[0].Tick);
			Assert.Equal(104, history[history.Count - 1].Tick);
		}

		[Fact]
		public void NewPlayer_HasEmptyHistory() {
			var g = new TestGame();

			var history = g.ResourceHistoryRepository.GetHistory(g.Player1);
			Assert.Empty(history);
		}
	}
}
