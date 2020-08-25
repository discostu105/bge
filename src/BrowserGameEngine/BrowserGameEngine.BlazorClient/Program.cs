using System;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using BrowserGameEngine.BlazorClient;
using Microsoft.AspNetCore.Components.Authorization;
using BrowserGameEngine.BlazorClient.Auth;

namespace BrowserGameEngine.BlazorClient {
	public class Program {
		public static async Task Main(string[] args) {
			var builder = WebAssemblyHostBuilder.CreateDefault(args);
			builder.RootComponents.Add<App>("app");

			builder.Services.AddHttpClient("ServerAPI", client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress))
				.AddHttpMessageHandler<LoggingHandler>();

			builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("ServerAPI"));

			builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
			builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();
			builder.Services.AddOptions();
			builder.Services.AddAuthorizationCore();
			await builder.Build().RunAsync();
		}
	}

	class LoggingHandler : DelegatingHandler {

		public LoggingHandler() {
		}

		protected override async Task<HttpResponseMessage> SendAsync(
			HttpRequestMessage request, System.Threading.CancellationToken cancellationToken) {
			var response = await base.SendAsync(request, cancellationToken);

			//if (!response.IsSuccessStatusCode) {
				Console.WriteLine("{0}\t{1}\t{2}", request.RequestUri,
					(int)response.StatusCode, response.Headers.Date);
			//}

			return response;
		}
	}

}
