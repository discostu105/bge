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

			builder.Services.AddScoped<RedirectIfUnauthorizedHandler>();
			builder.Services.AddHttpClient("ServerAPI", client => {
					client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
					client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest"); // required to force ASP.NET Core to return 401 instead of redirect
				})
				.AddHttpMessageHandler<RedirectIfUnauthorizedHandler>();

			builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("ServerAPI"));

			await builder.Build().RunAsync();
		}
	}
}
