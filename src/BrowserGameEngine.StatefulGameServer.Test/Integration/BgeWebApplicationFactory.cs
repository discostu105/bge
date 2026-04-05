using System.Collections.Generic;
using System.Net.Http;
using BrowserGameEngine.FrontendServer;
using Microsoft.Extensions.Configuration;
using BrowserGameEngine.Persistence;
using BrowserGameEngine.StatefulGameServer.GameRegistry;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace BrowserGameEngine.StatefulGameServer.Test.Integration {
	/// <summary>
	/// WebApplicationFactory for BGE integration tests.
	/// Replaces hosted services, authentication, and blob storage with test doubles.
	/// </summary>
	public class BgeWebApplicationFactory : WebApplicationFactory<Program> {
		protected override void ConfigureWebHost(IWebHostBuilder builder) {
			builder.UseEnvironment("Testing");

			builder.ConfigureAppConfiguration((_, config) => {
				config.AddInMemoryCollection(new Dictionary<string, string?> {
					["Bge:S3BucketName"] = "",
					["GitHub:ClientId"] = "",
					["GitHub:ClientSecret"] = "",
					["Bge:DevAuth"] = "true",
				});
			});

			builder.ConfigureServices(services => {
				// Remove background hosted services to prevent tick/persistence loops
				services.RemoveAll<IHostedService>();

				// Replace Discord notification service with null implementation
				services.RemoveAll<IGameNotificationService>();
				services.AddSingleton<IGameNotificationService, NullGameNotificationService>();

				// Replace IBlobStorage with in-memory to isolate persistence
				services.RemoveAll<IBlobStorage>();
				services.AddSingleton<IBlobStorage>(new InMemoryBlobStorage());

				// Register the test auth handler scheme
				services.AddAuthentication()
					.AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, null);

				// Override the default scheme AFTER all Program.cs registrations.
				// PostConfigure runs after all Configure actions so "Test" wins over "Cookies".
				services.PostConfigure<AuthenticationOptions>(options => {
					options.DefaultScheme = TestAuthHandler.SchemeName;
					options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
					options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
					options.DefaultForbidScheme = TestAuthHandler.SchemeName;
					options.DefaultSignInScheme = TestAuthHandler.SchemeName;
					options.DefaultSignOutScheme = TestAuthHandler.SchemeName;
				});
			});
		}

		/// <summary>
		/// Creates an unauthenticated HttpClient with auto-redirect disabled so 401/302 are returned directly.
		/// </summary>
		public new HttpClient CreateClient() {
			return CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
		}

		/// <summary>Creates an authenticated HttpClient for the given user ID.</summary>
		public HttpClient CreateAuthenticatedClient(string userId) {
			var client = CreateClient();
			client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, userId);
			return client;
		}
	}
}
