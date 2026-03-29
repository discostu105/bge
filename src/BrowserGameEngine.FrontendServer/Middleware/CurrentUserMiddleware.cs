using System.Security.Claims;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer;

namespace BrowserGameEngine.FrontendServer.Middleware {
	/// <summary>
	/// Middleware that populates CurrentUserContext from the authenticated user's claims or
	/// from a bearer token set by BearerTokenMiddleware. Runs after UseAuthentication().
	/// </summary>
	public class CurrentUserMiddleware {
		private readonly RequestDelegate _next;

		public CurrentUserMiddleware(RequestDelegate next) {
			_next = next;
		}

		public async Task InvokeAsync(
			HttpContext context,
			CurrentUserContext currentUserContext,
			UserRepository userRepository,
			UserRepositoryWrite userRepositoryWrite,
			PlayerRepository playerRepository) {
			// Bearer token path: BearerTokenMiddleware already resolved the PlayerId
			if (context.Items.TryGetValue("BearerPlayerId", out var bearerPlayerId) && bearerPlayerId is string bearerPlayerIdStr) {
				var playerId = PlayerIdFactory.Create(bearerPlayerIdStr);
				currentUserContext.Activate(playerId);
				await _next(context);
				return;
			}

			// Cookie/OAuth path: resolve User from GitHub claims
			if (context.User.Identity?.IsAuthenticated == true) {
				var githubIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
				var githubLoginClaim = context.User.FindFirst("urn:github:login")
					?? context.User.FindFirst(ClaimTypes.Name);
				var displayNameClaim = context.User.FindFirst(ClaimTypes.Name)
					?? githubLoginClaim;

				if (githubIdClaim != null) {
					var githubId = githubIdClaim.Value;
					var user = userRepositoryWrite.GetOrCreateUser(
						githubId: githubId,
						githubLogin: githubLoginClaim?.Value ?? githubId,
						displayName: displayNameClaim?.Value ?? githubId
					);

					// Select active player: first player belonging to this user
					var players = userRepository.GetPlayersForUser(user.UserId).ToList();
					if (players.Count > 0) {
						var selectedPlayerId = players[0].PlayerId;
						// Support BGE.SelectedPlayer cookie for multi-player accounts
						var selectedPlayerCookie = context.Request.Cookies["BGE.SelectedPlayer"];
						if (selectedPlayerCookie != null) {
							var cookieId = PlayerIdFactory.Create(selectedPlayerCookie);
							if (players.Any(p => p.PlayerId == cookieId)) {
								selectedPlayerId = cookieId;
							}
						}
						// Support X-Player-Id header (takes precedence over cookie)
						var playerIdHeader = context.Request.Headers["X-Player-Id"].FirstOrDefault();
						if (playerIdHeader != null) {
							var requestedId = PlayerIdFactory.Create(playerIdHeader);
							if (players.Any(p => p.PlayerId == requestedId)) {
								selectedPlayerId = requestedId;
							}
						}
						currentUserContext.Activate(selectedPlayerId);
						currentUserContext.UserId = user.UserId;
					} else {
						// Authenticated but no player yet; store UserId so create endpoint can use it
						currentUserContext.UserId = user.UserId;
					}
				}
			}

			await _next(context);
		}
	}
}
