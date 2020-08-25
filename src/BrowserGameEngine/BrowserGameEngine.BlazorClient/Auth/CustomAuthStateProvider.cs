using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;

namespace BrowserGameEngine.BlazorClient.Auth {
	public class CustomAuthStateProvider : AuthenticationStateProvider {
		public override Task<AuthenticationState> GetAuthenticationStateAsync() {

			var identity = new ClaimsIdentity(new[]
			{
			new Claim(ClaimTypes.Name, "mrfibuli"),
		}, "Fake authentication type");

			var user = new ClaimsPrincipal(identity);

			return Task.FromResult(new AuthenticationState(user));
		}
	}
}
