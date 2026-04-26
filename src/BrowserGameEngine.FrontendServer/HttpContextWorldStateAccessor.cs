using BrowserGameEngine.FrontendServer.Middleware;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using BrowserGameEngine.StatefulGameServer.GameRegistry;

namespace BrowserGameEngine.FrontendServer {
	/// <summary>
	/// Resolves the current request's WorldState dynamically. When the current
	/// HttpContext has a GameId set by CurrentGameMiddleware, returns that
	/// game's WorldState. Otherwise falls back to the default WorldState
	/// captured at startup — preserving the previous singleton behaviour for
	/// code paths that have no HttpContext (background hosted services, tick
	/// engine).
	/// </summary>
	public class HttpContextWorldStateAccessor : IWorldStateAccessor {
		private readonly IHttpContextAccessor httpContextAccessor;
		private readonly GameRegistry gameRegistry;
		private readonly WorldState defaultWorldState;

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
				var ctx = httpContextAccessor.HttpContext;
				if (ctx != null && ctx.Items.TryGetValue(CurrentGameMiddleware.GameIdItemKey, out var raw) && raw is GameId gameId) {
					var instance = gameRegistry.TryGetInstance(gameId);
					if (instance != null) return instance.WorldState;
				}
				return defaultWorldState;
			}
		}
	}
}
