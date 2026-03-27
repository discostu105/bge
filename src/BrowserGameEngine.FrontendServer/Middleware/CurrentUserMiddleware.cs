using System.Security.Claims;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer;

namespace BrowserGameEngine.FrontendServer.Middleware {
	/// <summary>
	/// Middleware that populates CurrentUserContext from the authenticated user's claims.
	/// Runs after UseAuthentication() so that HttpContext.User is available.
	/// </summary>
	public class CurrentUserMiddleware {
		private readonly RequestDelegate _next;

		public CurrentUserMiddleware(RequestDelegate next) {
			_next = next;
		}

		public async Task InvokeAsync(HttpContext context, CurrentUserContext currentUserContext, PlayerRepository playerRepository) {
			if (context.User.Identity?.IsAuthenticated == true) {
				var idClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
				if (idClaim != null) {
					var playerId = PlayerIdFactory.Create(idClaim.Value);
					if (playerRepository.Exists(playerId)) {
						currentUserContext.Activate(playerId);
					}
				}
			}
			await _next(context);
		}
	}
}
