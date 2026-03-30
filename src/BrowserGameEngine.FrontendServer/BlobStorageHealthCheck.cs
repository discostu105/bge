using BrowserGameEngine.Persistence;
using BrowserGameEngine.StatefulGameServer.GameRegistry;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BrowserGameEngine.FrontendServer;

public class BlobStorageHealthCheck : IHealthCheck
{
	private readonly IBlobStorage _storage;
	private readonly GameRegistry _gameRegistry;

	public BlobStorageHealthCheck(IBlobStorage storage, GameRegistry gameRegistry)
	{
		_storage = storage;
		_gameRegistry = gameRegistry;
	}

	public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
	{
		var activeGameCount = _gameRegistry.GetAllInstances().Count;
		var data = new Dictionary<string, object>
		{
			["activeGames"] = activeGameCount,
			["timestamp"] = DateTime.UtcNow
		};

		try {
			_storage.List("global").FirstOrDefault();
			data["blobStorage"] = "reachable";
			return Task.FromResult(HealthCheckResult.Healthy("OK", data));
		} catch (Exception ex) {
			data["blobStorage"] = "unreachable";
			return Task.FromResult(HealthCheckResult.Unhealthy("Blob storage unreachable", ex, data));
		}
	}
}
