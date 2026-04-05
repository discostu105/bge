using BrowserGameEngine.FrontendServer.Hubs;
using BrowserGameEngine.GameModel;
using Xunit;

namespace BrowserGameEngine.StatefulGameServer.Test.Events {
	public class PlayerConnectionTrackerTest {
		private static readonly PlayerId Player1 = PlayerIdFactory.Create("player0");
		private static readonly PlayerId Player2 = PlayerIdFactory.Create("player1");

		[Fact]
		public void Track_SingleConnection_ReturnsIt() {
			var tracker = new PlayerConnectionTracker();
			tracker.Track(Player1, "conn1");

			var connections = tracker.GetConnections(Player1);
			Assert.Single(connections);
			Assert.Equal("conn1", connections[0]);
		}

		[Fact]
		public void Track_MultipleConnections_ReturnsAll() {
			var tracker = new PlayerConnectionTracker();
			tracker.Track(Player1, "conn1");
			tracker.Track(Player1, "conn2");
			tracker.Track(Player1, "conn3");

			var connections = tracker.GetConnections(Player1);
			Assert.Equal(3, connections.Count);
		}

		[Fact]
		public void Untrack_RemovesConnection() {
			var tracker = new PlayerConnectionTracker();
			tracker.Track(Player1, "conn1");
			tracker.Track(Player1, "conn2");

			tracker.Untrack("conn1");

			var connections = tracker.GetConnections(Player1);
			Assert.Single(connections);
			Assert.Equal("conn2", connections[0]);
		}

		[Fact]
		public void Untrack_LastConnection_CleansUpPlayer() {
			var tracker = new PlayerConnectionTracker();
			tracker.Track(Player1, "conn1");

			tracker.Untrack("conn1");

			var connections = tracker.GetConnections(Player1);
			Assert.Empty(connections);
		}

		[Fact]
		public void Untrack_UnknownConnection_DoesNotThrow() {
			var tracker = new PlayerConnectionTracker();
			var ex = Record.Exception(() => tracker.Untrack("nonexistent"));
			Assert.Null(ex);
		}

		[Fact]
		public void GetConnections_UnknownPlayer_ReturnsEmpty() {
			var tracker = new PlayerConnectionTracker();
			var connections = tracker.GetConnections(Player1);
			Assert.Empty(connections);
		}

		[Fact]
		public void GetAllConnectionIds_ReturnsAllConnections() {
			var tracker = new PlayerConnectionTracker();
			tracker.Track(Player1, "conn1");
			tracker.Track(Player2, "conn2");
			tracker.Track(Player1, "conn3");

			var all = tracker.GetAllConnectionIds();
			Assert.Equal(3, all.Count);
		}

		[Fact]
		public void MultiplePlayers_IsolatedConnections() {
			var tracker = new PlayerConnectionTracker();
			tracker.Track(Player1, "conn1");
			tracker.Track(Player2, "conn2");

			Assert.Single(tracker.GetConnections(Player1));
			Assert.Equal("conn1", tracker.GetConnections(Player1)[0]);
			Assert.Single(tracker.GetConnections(Player2));
			Assert.Equal("conn2", tracker.GetConnections(Player2)[0]);
		}

		[Fact]
		public async System.Threading.Tasks.Task ConcurrentTrackUntrack_DoesNotThrow() {
			var tracker = new PlayerConnectionTracker();
			var tasks = new System.Threading.Tasks.Task[100];
			for (int i = 0; i < 100; i++) {
				int idx = i;
				tasks[i] = System.Threading.Tasks.Task.Run(() => {
					tracker.Track(Player1, $"conn{idx}");
					tracker.Untrack($"conn{idx}");
				});
			}
			await System.Threading.Tasks.Task.WhenAll(tasks);
			// All connections should be cleaned up
			Assert.Empty(tracker.GetConnections(Player1));
		}
	}
}
