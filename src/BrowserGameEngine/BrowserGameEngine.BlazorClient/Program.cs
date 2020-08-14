using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.Authorization;

namespace BrowserGameEngine.BlazorClient {
	public class Program {
		public static async Task Main(string[] args) {
			var builder = WebAssemblyHostBuilder.CreateDefault(args);
			builder.RootComponents.Add<App>("app");

			builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
			//builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();
			//builder.Services.AddOptions();
			//builder.Services.AddAuthorizationCore();

			builder.Services.AddOidcAuthentication<RemoteAuthenticationState,
				CustomUserAccount>(options => {

				})
				.AddAccountClaimsPrincipalFactory<RemoteAuthenticationState, CustomUserAccount, CustomAccountFactory>();


			//builder.Services.AddHttpClient("BrowserGameEngine.BlazorClient", client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress))
			//	.AddHttpMessageHandler<BaseAddressAuthorizationMessageHandler>();

			//// Supply HttpClient instances that include access tokens when making requests to the server project
			//builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("BrowserGameEngine.BlazorClient"));

			//builder.Services.AddApiAuthorization();


			await builder.Build().RunAsync();
		}
	}
}
