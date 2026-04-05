using System.Security.Claims;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace BrowserGameEngine.FrontendServer.Hubs;

/// <summary>
/// Central SignalR hub for real-time game events.
/// Server-to-client only — all player actions go through REST controllers.
/// </summary>
[Authorize]
public class GameHub : Hub
{
	private readonly PlayerConnectionTracker _tracker;
	private readonly UserRepository _userRepository;
	private readonly ILogger<GameHub> _logger;

	public GameHub(
		PlayerConnectionTracker tracker,
		UserRepository userRepository,
		ILogger<GameHub> logger)
	{
		_tracker = tracker;
		_userRepository = userRepository;
		_logger = logger;
	}

	public override Task OnConnectedAsync()
	{
		var playerId = ResolvePlayerId();
		if (playerId != null) {
			_tracker.Track(playerId, Context.ConnectionId);
			_logger.LogDebug("SignalR connected: {PlayerId} -> {ConnectionId}", playerId.Id, Context.ConnectionId);
		} else {
			_logger.LogWarning("SignalR connection without resolvable player: {ConnectionId}", Context.ConnectionId);
		}
		return base.OnConnectedAsync();
	}

	public override Task OnDisconnectedAsync(Exception? exception)
	{
		_tracker.Untrack(Context.ConnectionId);
		_logger.LogDebug("SignalR disconnected: {ConnectionId}", Context.ConnectionId);
		return base.OnDisconnectedAsync(exception);
	}

	private PlayerId? ResolvePlayerId()
	{
		// Bearer token path: BearerTokenMiddleware stores PlayerId in HttpContext.Items
		var httpContext = Context.GetHttpContext();
		if (httpContext?.Items.TryGetValue("BearerPlayerId", out var bearerObj) == true
			&& bearerObj is string bearerPlayerIdStr) {
			return PlayerIdFactory.Create(bearerPlayerIdStr);
		}

		// Cookie/OAuth path: resolve from GitHub claims
		var githubIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier);
		if (githubIdClaim == null) return null;

		var user = _userRepository.GetByGithubId(githubIdClaim.Value);
		if (user == null) return null;

		var players = _userRepository.GetPlayersForUser(user.UserId).ToList();
		if (players.Count == 0) return null;

		// Use the first player (multi-player selection not supported over SignalR)
		return players[0].PlayerId;
	}
}
