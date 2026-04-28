using System;
using System.Threading;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;

namespace BrowserGameEngine.StatefulGameServer {
	public class SingletonWorldStateAccessor : IWorldStateAccessor {
		private readonly WorldState defaultWorldState;
		private readonly AsyncLocal<WorldState?> ambient = new();

		public SingletonWorldStateAccessor(WorldState worldState) {
			defaultWorldState = worldState;
		}

		public WorldState WorldState => ambient.Value ?? defaultWorldState;

		public IDisposable PushAmbient(WorldState worldState) {
			var previous = ambient.Value;
			ambient.Value = worldState;
			return new AmbientScope(ambient, previous);
		}

		private sealed class AmbientScope : IDisposable {
			private readonly AsyncLocal<WorldState?> slot;
			private readonly WorldState? previous;
			private bool disposed;

			public AmbientScope(AsyncLocal<WorldState?> slot, WorldState? previous) {
				this.slot = slot;
				this.previous = previous;
			}

			public void Dispose() {
				if (disposed) return;
				disposed = true;
				slot.Value = previous;
			}
		}
	}
}
