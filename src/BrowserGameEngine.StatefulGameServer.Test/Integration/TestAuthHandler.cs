using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BrowserGameEngine.StatefulGameServer.Test.Integration {
	/// <summary>
	/// Authentication handler for integration tests. Reads X-Test-UserId header and
	/// creates a synthetic ClaimsPrincipal — no GitHub OAuth needed.
	/// </summary>
	public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions> {
		public const string SchemeName = "Test";
		public const string UserIdHeader = "X-Test-UserId";

		public TestAuthHandler(
			IOptionsMonitor<AuthenticationSchemeOptions> options,
			ILoggerFactory logger,
			UrlEncoder encoder)
			: base(options, logger, encoder) { }

		protected override Task<AuthenticateResult> HandleAuthenticateAsync() {
			if (!Request.Headers.TryGetValue(UserIdHeader, out var userIdValues)
				|| string.IsNullOrWhiteSpace(userIdValues.ToString())) {
				return Task.FromResult(AuthenticateResult.NoResult());
			}

			var userId = userIdValues.ToString();
			var claims = new[] {
				new Claim(ClaimTypes.NameIdentifier, userId),
				new Claim(ClaimTypes.Name, userId),
				new Claim("urn:github:login", userId),
			};
			var identity = new ClaimsIdentity(claims, SchemeName);
			var principal = new System.Security.Claims.ClaimsPrincipal(identity);
			var ticket = new AuthenticationTicket(principal, SchemeName);
			return Task.FromResult(AuthenticateResult.Success(ticket));
		}
	}
}
