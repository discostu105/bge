using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BrowserGameEngine.GameDefinition;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace BrowserGameEngine.Server {
	public class Program {
		public static int Main(string[] args) {
			Log.Logger = new LoggerConfiguration()
				.MinimumLevel.Debug()
				.MinimumLevel.Override("Microsoft", LogEventLevel.Information)
				.Enrich.FromLogContext()
				.WriteTo.Console()
				.CreateLogger();
			try {
				Log.Information("Starting web host");
				CreateHostBuilder(args).Build().Run();
				return 0;
			} catch (Exception ex) {
				Log.Fatal(ex, "Host terminated unexpectedly");
				return 1;
			} finally {
				Log.CloseAndFlush();
			}
		}

		public static IHostBuilder CreateHostBuilder(string[] args) =>
			Host.CreateDefaultBuilder(args)
				.UseSerilog()
				.ConfigureWebHostDefaults(webBuilder => {
					webBuilder.UseStartup<Startup>();
				});
	}
}
