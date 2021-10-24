using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Linq;
using System;
using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.StatefulGameServer;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.FrontendServer;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using BrowserGameEngine.StatefulGameServer.GameTicks;
using BrowserGameEngine.Persistence;
using System.IO;
using System.Runtime.InteropServices;
using BrowserGameEngine.GameDefinition.SCO;
using Microsoft.Extensions.Options;
using OpenTelemetry.Trace;
using Prometheus;
using Prometheus.DotNetRuntime;

namespace BrowserGameEngine.FrontendServer {
	public class Startup {
		public Startup(IConfiguration configuration) {
			Configuration = configuration;
		}

		public IConfiguration Configuration { get; }

		// This method gets called by the runtime. Use this method to add services to the container.
		// For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
		public void ConfigureServices(IServiceCollection services) {
			services.Configure<BgeOptions>(Configuration.GetSection(BgeOptions.Position));
			services.AddControllersWithViews();
			services.AddRazorPages();
			services.AddLogging();
			
			services.AddOpenTelemetryTracing((builder) => builder
				.AddAspNetCoreInstrumentation()
				.AddJaegerExporter()
				//.AddJaegerExporter(jaegerOptions =>
				//{
				//	jaegerOptions.AgentHost = this.Configuration.GetValue<string>("Jaeger:Host");
				//	jaegerOptions.AgentPort = this.Configuration.GetValue<int>("Jaeger:Port");
				//})
			);

			services.AddAuthentication(options => {
				options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
				options.RequireAuthenticatedSignIn = true;
			})
			.AddCookie(options => {
				options.Cookie.Name = "BGE.AuthCookie";
			})
			.AddDiscord(options => {
				options.ClientId = "743803200787709953";
				options.ClientSecret = "JgcbikjBvxVuUNrgTQWark-VCbrIcrym";
				options.SaveTokens = true;
				//options.ForwardSignIn = "/fwdsignin";
				//options.ForwardDefault = "/fwdefault";
				//options.ForwardChallenge = "/fwdchallenge";
				//options.ForwardChallenge = "/fwdauth";
				options.Events.OnTicketReceived = ctx => {
					
					Console.WriteLine("OnTicketReceived");
					return Task.CompletedTask;
				};
				//options.Events.OnAccessDenied = ctx => {
				//	Console.WriteLine("OnAccessDenied");
				//	return Task.CompletedTask;
				//};
				//options.Events.OnCreatingTicket = ctx => {
				//	Console.WriteLine("OnCreatingTicket");
				//	return Task.CompletedTask;
				//};
				//options.Events.OnRedirectToAuthorizationEndpoint = ctx => {
				//	Console.WriteLine("OnRedirectToAuthorizationEndpoint");
				//	return Task.CompletedTask;
				//};
				//options.Events.OnRemoteFailure = ctx => {
				//	Console.WriteLine("OnRemoteFailure");
				//	return Task.CompletedTask;
				//};
			});

			services.ConfigureApplicationCookie(options => {
				options.Events.OnRedirectToLogin = context => {
					context.Response.Headers["Location"] = context.RedirectUri;
					context.Response.StatusCode = 401;
					return Task.CompletedTask;
				};
			});

			services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Latest);

			ConfigureGameServices(services).Wait();
		}

		private async Task ConfigureGameServices(IServiceCollection services) {
			var gameDefFactory = new StarcraftOnlineGameDefFactory();
			var stateFactory = new StarcraftOnlineWorldStateFactory();
			var gameDef = gameDefFactory.CreateGameDef();
			new GameDefVerifier().Verify(gameDef);
			services.AddSingleton<GameDef>(gameDef);

			var storage = new FileStorage(new DirectoryInfo("storage")); // todo: make this configurable

			var worldState = stateFactory.CreateDevWorldState();
			new WorldStateVerifier().Verify(gameDef, worldState); // todo: maybe make this more graceful.

			await services.AddGameServer(storage, worldState); // todo: init state for non-development
			//services.AddSingleton(CurrentUserContext.Create(playerId: "discostu#1")); // for dev purposes only.
			services.AddSingleton(CurrentUserContext.Inactive());

			services.Configure<HostOptions>(opts =>
				opts.ShutdownTimeout = TimeSpan.FromMinutes(10) // large shutdown timeout to allow persistance to save gamestate
			);
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHostApplicationLifetime lifetime) {
			if (env.IsDevelopment()) {
				app.UseDeveloperExceptionPage();
				app.UseWebAssemblyDebugging();
			} else {
				app.UseExceptionHandler("/Error");
				// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
				app.UseHsts();
			}

			app.UseHttpsRedirection();
			app.UseBlazorFrameworkFiles();
			app.UseStaticFiles();

			app.UseRouting();
			app.UseHttpMetrics();

			IDisposable collector = DotNetRuntimeStatsBuilder
				.Customize()
				.WithContentionStats()
				.WithJitStats()
				.WithThreadPoolStats()
				.WithGcStats()
				.WithExceptionStats()
				.StartCollecting();

			app.UseAuthentication();
			app.UseAuthorization();

			app.UseEndpoints(endpoints => {
				endpoints.MapDefaultControllerRoute();
				endpoints.MapRazorPages();
				endpoints.MapControllers();
				endpoints.MapFallbackToFile("index.html");
				endpoints.MapMetrics();
			});
		}
	}
}
