using Amazon.S3;
using BrowserGameEngine.FrontendServer;
using BrowserGameEngine.FrontendServer.Middleware;
using BrowserGameEngine.FrontendServer.Services;
using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameDefinition.SCO;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.Persistence;
using BrowserGameEngine.Persistence.S3;
using BrowserGameEngine.StatefulGameServer;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using BrowserGameEngine.StatefulGameServer.GameTicks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;
using Prometheus;
using Prometheus.DotNetRuntime;
using Serilog;
using Serilog.Events;
using System.Linq;
using System.Threading.RateLimiting;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog();

/*
/* Add services to the container.
 */
builder.Services.AddHostedService<GameTickTimerService>();
builder.Services.AddHostedService<PersistenceHostedService>();
builder.Services.AddHostedService<GlobalPersistenceHostedService>();
builder.Services.AddHostedService<GameLifecycleService>();

builder.Services.Configure<BgeOptions>(builder.Configuration.GetSection(BgeOptions.Position));
builder.Services.AddHttpClient();
builder.Services.AddSingleton<BrowserGameEngine.StatefulGameServer.GameRegistry.IGameNotificationService, BrowserGameEngine.FrontendServer.Services.DiscordWebhookNotificationService>();
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddLogging();
builder.Services.AddHealthChecks()
    .AddCheck<BlobStorageHealthCheck>("blob-storage");
builder.Services.AddOpenApi();

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddOtlpExporter()
    );

builder.Services.AddRateLimiter(options => {
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context => {
        var apiKeyHash = context.Items["ApiKeyHash"] as string;
        var partitionKey = apiKeyHash ?? context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions {
            PermitLimit = 60,
            Window = TimeSpan.FromMinutes(1),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0
        });
    });
    options.RejectionStatusCode = 429;
});

builder.Services.AddAuthentication(options => {
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.RequireAuthenticatedSignIn = true;
})
    .AddCookie(options => {
        options.Cookie.Name = "BGE.AuthCookie";
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.LoginPath = "/signin";
        options.Events.OnRedirectToLogin = context => {
            if (context.Request.Headers["X-Requested-With"] == "XMLHttpRequest") {
                context.Response.StatusCode = 401;
                return Task.CompletedTask;
            }
            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
    });

var githubClientId = builder.Configuration["GitHub:ClientId"];
var githubClientSecret = builder.Configuration["GitHub:ClientSecret"];
if (!string.IsNullOrWhiteSpace(githubClientId) && !string.IsNullOrWhiteSpace(githubClientSecret)) {
    builder.Services.AddAuthentication().AddGitHub(options => {
        options.ClientId = githubClientId;
        options.ClientSecret = githubClientSecret;
        options.SaveTokens = true;
        options.CorrelationCookie.SameSite = SameSiteMode.Lax;
        options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
    });
} else {
    Log.Warning("GitHub OAuth is disabled because GitHub:ClientId or GitHub:ClientSecret is not configured.");
}


await ConfigureGameServices(builder.Services);

/*
/* Configure the HTTP request pipeline.
 */
var app = builder.Build();

// Wire tick engine into game instances (Phase 1: single game)
var tickEngine = app.Services.GetRequiredService<GameTickEngine>();
foreach (var instance in app.Services.GetRequiredService<BrowserGameEngine.StatefulGameServer.GameRegistry.GameRegistry>().GetAllInstances()) {
	instance.SetTickEngine(tickEngine);
}

var forwardedHeadersOptions = new ForwardedHeadersOptions {
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};
forwardedHeadersOptions.KnownIPNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeadersOptions);

if (app.Environment.IsDevelopment()) {
    app.UseDeveloperExceptionPage();
    app.UseWebAssemblyDebugging();
    var urls = app.Configuration["ASPNETCORE_URLS"] ?? string.Empty;
    if (urls.Contains("https://", StringComparison.OrdinalIgnoreCase)) {
        app.UseHttpsRedirection();
    }
}
app.UseExceptionHandler("/Error");
app.MapOpenApi();
if (app.Configuration["Bge:EnableSwagger"] == "true") {
    app.UseSwaggerUI(c => {
        c.SwaggerEndpoint("/openapi/v1.json", "BGE API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseBlazorFrameworkFiles();
app.UseStaticFiles(new StaticFileOptions {
	OnPrepareResponse = ctx => {
		if (string.Equals(ctx.File.Name, "index.html", StringComparison.OrdinalIgnoreCase)) {
			ctx.Context.Response.Headers.CacheControl = "no-store";
		}
	}
});

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
app.UseMiddleware<BearerTokenMiddleware>();
app.UseRateLimiter();
app.UseMiddleware<CurrentUserMiddleware>();
app.UseAuthorization();

app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions {
    ResponseWriter = async (ctx, report) => {
        ctx.Response.ContentType = "application/json";
        var result = System.Text.Json.JsonSerializer.Serialize(new {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new {
                name = e.Key,
                status = e.Value.Status.ToString(),
                data = e.Value.Data
            }),
            totalDuration = report.TotalDuration.TotalMilliseconds
        });
        await ctx.Response.WriteAsync(result);
    }
});
app.MapDefaultControllerRoute();
app.MapRazorPages();
app.MapControllers();
app.MapMetrics();
app.MapFallbackToFile("index.html", new StaticFileOptions {
	OnPrepareResponse = ctx => {
		ctx.Context.Response.Headers.CacheControl = "no-store";
	}
});

app.Run();


async Task ConfigureGameServices(IServiceCollection services) {
    var gameDefFactory = new StarcraftOnlineGameDefFactory();
    IWorldStateFactory stateFactory = new StarcraftOnlineWorldStateFactory();
    var gameDef = gameDefFactory.CreateGameDef();
    new GameDefVerifier().Verify(gameDef);

    var s3BucketName = builder.Configuration["Bge:S3BucketName"];
    IBlobStorage storage = !string.IsNullOrEmpty(s3BucketName)
        ? new S3Storage(new AmazonS3Client(), s3BucketName, builder.Configuration["Bge:S3KeyPrefix"] ?? "")
        : new FileStorage(new DirectoryInfo("storage"));

    var defaultWorldState = stateFactory.CreateDevWorldState();
    new WorldStateVerifier().Verify(gameDef, defaultWorldState);

    var serializer = new GameStateJsonSerializer();
    var persistenceService = new PersistenceService(storage, serializer);
    var globalSerializer = new GlobalStateJsonSerializer();
    var globalPersistenceService = new GlobalPersistenceService(storage, globalSerializer);

    var migrationLogger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger<MigrationService>();
    var migrationService = new MigrationService(storage, persistenceService, globalPersistenceService, migrationLogger);
    await migrationService.MigrateIfNeeded();

    GlobalState globalState;
    if (globalPersistenceService.GlobalStateExists()) {
        var globalStateImm = await globalPersistenceService.LoadGlobalState();
        globalState = globalStateImm.ToMutable();
    } else {
        globalState = new GlobalState();
    }

    var gameRegistry = new BrowserGameEngine.StatefulGameServer.GameRegistry.GameRegistry(globalState);

    foreach (var gameRecord in globalState.GetGames().Where(g => g.Status != GameStatus.Finished)) {
        WorldStateImmutable wsImm;
        if (persistenceService.GameStateExists(gameRecord.GameId)) {
            wsImm = await persistenceService.LoadGameState(gameRecord.GameId);
        } else {
            wsImm = defaultWorldState;
        }
        var ws = wsImm.ToMutable();
        var gd = new StarcraftOnlineGameDefFactory().CreateGameDef();
        gameRegistry.Register(new BrowserGameEngine.StatefulGameServer.GameRegistry.GameInstance(gameRecord, ws, gd));
    }

    if (!gameRegistry.GetAllInstances().Any()) {
        var ws = defaultWorldState.ToMutable();
        var gameRecord = new GameRecordImmutable(
            new GameId("default"), "Default Game", "sco", GameStatus.Active,
            DateTime.UtcNow, DateTime.UtcNow + TimeSpan.FromDays(3650), TimeSpan.FromSeconds(10));
        var gd = new StarcraftOnlineGameDefFactory().CreateGameDef();
        gameRegistry.Register(new BrowserGameEngine.StatefulGameServer.GameRegistry.GameInstance(gameRecord, ws, gd));
    }

    services.AddGameServer(storage, gameRegistry, stateFactory);

    var bucketDisplay = string.IsNullOrEmpty(s3BucketName) ? "(local FileStorage)" : s3BucketName;
    Log.Information("Startup: games loaded={GameCount}, users={UserCount}, storage={Storage}",
        gameRegistry.GetAllInstances().Count,
        globalState.ToImmutable().Users.Count,
        bucketDisplay);

    services.AddScoped(_ => CurrentUserContext.Inactive());

    services.Configure<HostOptions>(opts =>
        opts.ShutdownTimeout = TimeSpan.FromMinutes(10) // large shutdown timeout to allow persistance to save gamestate
    );
}
