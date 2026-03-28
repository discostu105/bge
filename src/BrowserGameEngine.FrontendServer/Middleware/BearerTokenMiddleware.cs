using System.Security.Cryptography;
using System.Text;
using BrowserGameEngine.StatefulGameServer;
using Microsoft.Net.Http.Headers;

namespace BrowserGameEngine.FrontendServer.Middleware {
	/// <summary>
	/// Middleware that authenticates API requests using a Bearer token in the format bge_k_...
	/// Resolves the token to a Player and stores the PlayerId in HttpContext.Items for CurrentUserMiddleware.
	/// Must run after UseAuthentication() but before CurrentUserMiddleware.
	/// </summary>
	public class BearerTokenMiddleware {
		private const string ApiKeyPrefix = "bge_k_";
		private readonly RequestDelegate _next;

		public BearerTokenMiddleware(RequestDelegate next) {
			_next = next;
		}

		public async Task InvokeAsync(HttpContext context, UserRepository userRepository) {
			var authHeader = context.Request.Headers[HeaderNames.Authorization].FirstOrDefault();
			if (authHeader != null && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) {
				var token = authHeader["Bearer ".Length..].Trim();
				if (token.StartsWith(ApiKeyPrefix, StringComparison.Ordinal)) {
					var hash = HashApiKey(token);
					context.Items["ApiKeyHash"] = hash;
					var player = userRepository.GetPlayerByApiKeyHash(hash);
					if (player != null) {
						context.Items["BearerPlayerId"] = player.PlayerId.Id;
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
