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

namespace BrowserGameEngine.Server {
	public class Startup {
		public Startup(IConfiguration configuration) {
			Configuration = configuration;
		}

		public IConfiguration Configuration { get; }

		// This method gets called by the runtime. Use this method to add services to the container.
		// For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
		public void ConfigureServices(IServiceCollection services) {
			services.AddControllersWithViews();
			services.AddRazorPages();

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
			services.ConfigureApplicationCookie(o => {
				o.Events = new CookieAuthenticationEvents() {
					OnRedirectToLogin = (ctx) => {
						if (ctx.Request.Path.StartsWithSegments("/api") && ctx.Response.StatusCode == 200) {
							ctx.Response.StatusCode = 401;
						}
						return Task.CompletedTask;
					},
					OnRedirectToAccessDenied = (ctx) => {
						if (ctx.Request.Path.StartsWithSegments("/api") && ctx.Response.StatusCode == 200) {
							ctx.Response.StatusCode = 403;
						}
						return Task.CompletedTask;
					}
				};
			});

			services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Latest);

			ConfigureGameServices(services);
		}

		private void ConfigureGameServices(IServiceCollection services) {
			services.AddSingleton(GameDefFactory.CreateStarcraftOnline());
			services.AddGameServer(DemoWorldStateFactory.CreateStarCraftOnlineDemoWorldState1());
			services.AddSingleton(CurrentUserContext.Create(playerId: "discostu", playerTypeId: "terran")); // for dev purposes only.
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
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

			app.UseAuthentication();
			app.UseAuthorization();

			app.UseEndpoints(endpoints => {
				endpoints.MapDefaultControllerRoute();
				endpoints.MapRazorPages();
				endpoints.MapControllers();
				endpoints.MapFallbackToFile("index.html");
			});
		}
	}
}
