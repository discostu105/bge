using System.Diagnostics;

namespace BrowserGameEngine.FrontendServer.Middleware;

public class RequestDurationMiddleware
{
	private readonly RequestDelegate _next;
	private readonly ILogger<RequestDurationMiddleware> _logger;

	public RequestDurationMiddleware(RequestDelegate next, ILogger<RequestDurationMiddleware> logger)
	{
		_next = next;
		_logger = logger;
	}

	public async Task InvokeAsync(HttpContext context)
	{
		var sw = Stopwatch.StartNew();
		await _next(context);
		sw.Stop();

		// Only log API requests; skip health, metrics, static files
		var path = context.Request.Path.Value ?? "";
		if (path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase)) {
			_logger.LogInformation("HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs}ms",
				context.Request.Method,
				path,
				context.Response.StatusCode,
				sw.ElapsedMilliseconds);
		}
	}
}
