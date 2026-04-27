using System;
using System.Threading;
using BrowserGameEngine.FrontendServer.Middleware;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using BrowserGameEngine.StatefulGameServer.GameRegistry;

namespace BrowserGameEngine.FrontendServer {
	/// <summary>
	/// Resolves the current request's WorldState dynamically. Resolution order:
	/// 1) An ambient WorldState pushed via <see cref="PushAmbient"/> — used by
	///    background services (game tick timer, lifecycle service) that have no
	///    HttpContext but must direct singleton repositories at a specific game.
	/// 2) The HttpContext's GameId set by CurrentGameMiddleware (the X-Game-Id
	///    header) — used by HTTP-scoped requests.
	/// 3) The default WorldState captured at startup — preserves singleton
	///    behaviour for code paths with neither (e.g. early startup).
	/// </summary>
	public class HttpContextWorldStateAccessor : IWorldStateAccessor {
		private readonly IHttpContextAccessor httpContextAccessor;
		private readonly GameRegistry gameRegistry;
		private readonly WorldState defaultWorldState;
		private readonly AsyncLocal<WorldState?> ambient = new();

		public HttpContextWorldStateAccessor(
			IHttpContextAccessor httpContextAccessor,
			GameRegistry gameRegistry,
			WorldState defaultWorldState) {
			this.httpContextAccessor = httpContextAccessor;
			this.gameRegistry = gameRegistry;
			this.defaultWorldState = defaultWorldState;
		}

		public WorldState WorldState {
			get {
				var ambientValue = ambient.Value;
				if (ambientValue != null) return ambientValue;
				var ctx = httpContextAccessor.HttpContext;
				if (ctx != null && ctx.Items.TryGetValue(CurrentGameMiddleware.GameIdItemKey, out var raw) && raw is GameId gameId) {
					var instance = gameRegistry.TryGetInstance(gameId);
					if (instance != null) return instance.WorldState;
				}
				return defaultWorldState;
			}
		}

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
