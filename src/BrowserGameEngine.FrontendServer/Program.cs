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
using Microsoft.AspNetCore.ResponseCompression;
using OpenTelemetry.Trace;
using Prometheus;
using Prometheus.DotNetRuntime;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

var rookoutToken = builder.Configuration["Rookout:Token"];
if (!string.IsNullOrEmpty(rookoutToken)) {
    Rook.RookOptions rookOptions = new Rook.RookOptions() {
        token = rookoutToken,
        labels = new Dictionary<string, string> { { "env", builder.Environment.EnvironmentName } }
    };
    Rook.API.Start(rookOptions);
}
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

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddJaegerExporter()
    );

builder.Services.AddAuthentication(options => {
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.RequireAuthenticatedSignIn = true;
})
    .AddCookie(options => {
        options.Cookie.Name = "BGE.AuthCookie";
    })
    .AddDiscord(options => {
        options.ClientId = builder.Configuration["Discord:ClientId"] ?? throw new InvalidOperationException("Discord:ClientId not configured");
        options.ClientSecret = builder.Configuration["Discord:ClientSecret"] ?? throw new InvalidOperationException("Discord:ClientSecret not configured");
        options.SaveTokens = true;
    });

builder.Services.ConfigureApplicationCookie(options => {
    options.Events.OnRedirectToLogin = context => {
        context.Response.Headers["Location"] = context.RedirectUri;
        context.Response.StatusCode = 401;
        return Task.CompletedTask;
    };
});

await ConfigureGameServices(builder.Services);

/*
/* Configure the HTTP request pipeline.
 */
var app = builder.Build();

app.UseForwardedHeaders(new ForwardedHeadersOptions {
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

if (app.Environment.IsDevelopment()) {
    app.UseDeveloperExceptionPage();
    app.UseWebAssemblyDebugging();
} else {
    app.UseExceptionHandler("/Error");
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
app.UseMiddleware<CurrentUserMiddleware>();
app.UseAuthorization();

app.MapHealthChecks("/health");

app.UseEndpoints(endpoints => {
    endpoints.MapDefaultControllerRoute();
    endpoints.MapRazorPages();
    endpoints.MapControllers();
    endpoints.MapFallbackToFile("index.html");
    endpoints.MapMetrics();
});

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