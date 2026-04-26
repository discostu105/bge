using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.GameRegistry;

namespace BrowserGameEngine.FrontendServer.Middleware {
	/// <summary>
	/// Reads the X-Game-Id request header and stores the corresponding GameId in
	/// HttpContext.Items so HttpContextWorldStateAccessor can resolve the right
	/// WorldState. Silently ignores missing headers (callers that don't need a
	/// game context — e.g. /api/games or unauthenticated pages — work as before).
	/// Returns 400 when the header is set but the game id is unknown.
	/// Validates against the persisted game catalog (GlobalState), not the in-memory
	/// registry: finished games are evicted from the registry to free memory, but
	/// their lobby/results endpoints must still be reachable for clients holding
	/// URLs like /games/{id}/briefing or /games/{id}/results.
	/// </summary>
	public class CurrentGameMiddleware {
		public const string GameIdHeader = "X-Game-Id";
		public const string GameIdItemKey = "BGE.GameId";

		private readonly RequestDelegate next;

		public CurrentGameMiddleware(RequestDelegate next) {
			this.next = next;
		}

		public async Task InvokeAsync(HttpContext context, GameRegistry gameRegistry) {
			var headerValue = context.Request.Headers[GameIdHeader].FirstOrDefault();
			if (!string.IsNullOrEmpty(headerValue)) {
				if (!gameRegistry.GlobalState.GameExists(headerValue)) {
					context.Response.StatusCode = StatusCodes.Status400BadRequest;
					await context.Response.WriteAsync($"Unknown game id: {headerValue}");
					return;
				}
				context.Items[GameIdItemKey] = new GameId(headerValue);
			}
			await next(context);
		}
	}
}
