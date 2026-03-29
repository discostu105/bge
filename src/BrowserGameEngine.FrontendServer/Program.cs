using Amazon.S3;
using BrowserGameEngine.FrontendServer;
using BrowserGameEngine.FrontendServer.Middleware;
using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameDefinition.SCO;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.Persistence;
using BrowserGameEngine.Persistence.S3;
using BrowserGameEngine.StatefulGameServer;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using OpenTelemetry.Trace;
using Prometheus;
using Prometheus.DotNetRuntime;
using Serilog;
using Serilog.Events;
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

builder.Services.Configure<BgeOptions>(builder.Configuration.GetSection(BgeOptions.Position));
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddLogging();
builder.Services.AddHealthChecks();
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
    })
    .AddGitHub(options => {
        options.ClientId = builder.Configuration["GitHub:ClientId"] ?? throw new InvalidOperationException("GitHub:ClientId not configured");
        options.ClientSecret = builder.Configuration["GitHub:ClientSecret"] ?? throw new InvalidOperationException("GitHub:ClientSecret not configured");
        options.SaveTokens = true;
        options.CorrelationCookie.SameSite = SameSiteMode.Lax;
        options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
    });


await ConfigureGameServices(builder.Services);

/*
/* Configure the HTTP request pipeline.
 */
var app = builder.Build();

var forwardedHeadersOptions = new ForwardedHeadersOptions {
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};
forwardedHeadersOptions.KnownNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeadersOptions);

if (app.Environment.IsDevelopment()) {
    app.UseDeveloperExceptionPage();
    app.UseWebAssemblyDebugging();
    app.UseHttpsRedirection();
}
app.UseExceptionHandler("/Error");
app.MapOpenApi();

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
app.UseMiddleware<BearerTokenMiddleware>();
app.UseRateLimiter();
app.UseMiddleware<CurrentUserMiddleware>();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapDefaultControllerRoute();
app.MapRazorPages();
app.MapControllers();
app.MapMetrics();
app.MapFallbackToFile("index.html");

app.Run();


async Task ConfigureGameServices(IServiceCollection services) {
    var gameDefFactory = new StarcraftOnlineGameDefFactory();
    var stateFactory = new StarcraftOnlineWorldStateFactory();
    var gameDef = gameDefFactory.CreateGameDef();
    new GameDefVerifier().Verify(gameDef);
    services.AddSingleton<GameDef>(gameDef);

    var s3BucketName = builder.Configuration["Bge:S3BucketName"];
    IBlobStorage storage = !string.IsNullOrEmpty(s3BucketName)
        ? new S3Storage(new AmazonS3Client(), s3BucketName, builder.Configuration["Bge:S3KeyPrefix"] ?? "")
        : new FileStorage(new DirectoryInfo("storage"));

    var worldState = stateFactory.CreateDevWorldState();
    new WorldStateVerifier().Verify(gameDef, worldState); // todo: maybe make this more graceful.

    await services.AddGameServer(storage, worldState); // todo: init state for non-development

    services.AddScoped(_ => CurrentUserContext.Inactive());

    services.Configure<HostOptions>(opts =>
        opts.ShutdownTimeout = TimeSpan.FromMinutes(10) // large shutdown timeout to allow persistance to save gamestate
    );
}
