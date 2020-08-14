using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication.Internal;

namespace BrowserGameEngine.BlazorClient {
	public class CustomAccountFactory
		: AccountClaimsPrincipalFactory<CustomUserAccount> {
		public CustomAccountFactory(NavigationManager navigationManager,
			IAccessTokenProviderAccessor accessor) : base(accessor) {
		}

		public async override ValueTask<ClaimsPrincipal> CreateUserAsync(
			CustomUserAccount account, RemoteAuthenticationUserOptions options) {
			var initialUser = await base.CreateUserAsync(account, options);

			if (initialUser.Identity.IsAuthenticated) {
				foreach (var value in account.AuthenticationMethod) {
					((ClaimsIdentity)initialUser.Identity)
						.AddClaim(new Claim("amr", value));
				}
			}

			return initialUser;
		}
	}
}