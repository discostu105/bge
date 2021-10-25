using System;
using System.Net.Http;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using BrowserGameEngine.BlazorClient;
using BrowserGameEngine.BlazorClient.Auth;

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

