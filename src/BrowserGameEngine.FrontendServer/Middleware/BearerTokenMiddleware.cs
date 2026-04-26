using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BrowserGameEngine.StatefulGameServer;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Net.Http.Headers;

namespace BrowserGameEngine.FrontendServer.Middleware {
	/// <summary>
	/// Middleware that authenticates API requests using a Bearer token in the format bge_k_...
	/// Resolves the token to a Player and stores the PlayerId in HttpContext.Items for CurrentUserMiddleware.
	/// Also sets HttpContext.User and the IAuthenticateResultFeature so that [Authorize] passes.
	/// Must run after UseAuthentication() but before CurrentUserMiddleware.
	/// </summary>
	public class BearerTokenMiddleware {
		private const string ApiKeyPrefix = "bge_k_";
		private readonly RequestDelegate _next;

		public BearerTokenMiddleware(RequestDelegate next) {
			_next = next;
		}

		public async Task InvokeAsync(HttpContext context, UserRepository userRepository, UserRepositoryWrite userRepositoryWrite) {
			var authHeader = context.Request.Headers[HeaderNames.Authorization].FirstOrDefault();
			if (authHeader != null && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) {
				var token = authHeader["Bearer ".Length..].Trim();
				if (token.StartsWith(ApiKeyPrefix, StringComparison.Ordinal)) {
					var hash = HashApiKey(token);
					context.Items["ApiKeyHash"] = hash;
					var player = userRepository.GetPlayerByApiKeyHash(hash);
					if (player != null) {
						userRepositoryWrite.TouchApiKey(hash);
						context.Items["BearerPlayerId"] = player.PlayerId.Id;
						var claims = new[] {
							new Claim(ClaimTypes.NameIdentifier, player.PlayerId.Id),
							new Claim(ClaimTypes.AuthenticationMethod, "ApiKey"),
						};
						var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "ApiKey"));
						context.User = principal;
						// Update the auth result feature so UseAuthorization() sees the authenticated result
						// instead of the stale cookie-scheme NoResult from UseAuthentication().
						var ticket = new AuthenticationTicket(principal, "ApiKey");
						var feature = context.Features.Get<IAuthenticateResultFeature>();
						if (feature != null) {
							feature.AuthenticateResult = AuthenticateResult.Success(ticket);
						}
					}
				}
			}
			await _next(context);
		}

		internal static string HashApiKey(string apiKey) {
			var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(apiKey));
			return Convert.ToHexStringLower(bytes);
		}
	}
}
