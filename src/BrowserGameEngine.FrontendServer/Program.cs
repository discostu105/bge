using BrowserGameEngine.FrontendServer;
using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameDefinition.SCO;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.Persistence;
using BrowserGameEngine.StatefulGameServer;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.ResponseCompression;
using OpenTelemetry.Trace;
using Prometheus;
using Prometheus.DotNetRuntime;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
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

builder.Services.AddOpenTelemetryTracing((builder) => builder
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
        options.ClientId = "743803200787709953";
        options.ClientSecret = "JgcbikjBvxVuUNrgTQWark-VCbrIcrym";
        options.SaveTokens = true;
        options.Events.OnTicketReceived = ctx => {
            Console.WriteLine("OnTicketReceived");
            return Task.CompletedTask;
        };
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

if (app.Environment.IsDevelopment()) {
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

app.Run();


async Task ConfigureGameServices(IServiceCollection services) {
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