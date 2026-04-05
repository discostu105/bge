using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.Persistence;
using BrowserGameEngine.StatefulGameServer;
using BrowserGameEngine.StatefulGameServer.ActionFeed;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using BrowserGameEngine.StatefulGameServer.GameTicks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrowserGameEngine.FrontendServer.Controllers;

[ApiController]
[Authorize]
[Route("api/admin")]
public class AdminController : ControllerBase
{
	private readonly ILogger<AdminController> logger;
	private readonly IOptions<BgeOptions> options;
	private readonly PlayerRepository playerRepository;
	private readonly PlayerRepositoryWrite playerRepositoryWrite;
	private readonly ResourceRepository resourceRepository;
	private readonly ResourceRepositoryWrite resourceRepositoryWrite;
	private readonly AssetRepositoryWrite assetRepositoryWrite;
	private readonly UnitRepositoryWrite unitRepositoryWrite;
	private readonly ActionQueueRepository actionQueueRepository;
	private readonly GameTickEngine gameTickEngine;
	private readonly WorldState worldState;
	private readonly GameStateJsonSerializer serializer;
	private readonly IBlobStorage storage;
	private readonly IActionLogger actionLogger;
	private readonly GameDef gameDef;
	private readonly ScoreRepository scoreRepository;
	private readonly MarketRepository marketRepository;

	public AdminController(
		ILogger<AdminController> logger,
		IOptions<BgeOptions> options,
		PlayerRepository playerRepository,
		PlayerRepositoryWrite playerRepositoryWrite,
		ResourceRepository resourceRepository,
		ResourceRepositoryWrite resourceRepositoryWrite,
		AssetRepositoryWrite assetRepositoryWrite,
		UnitRepositoryWrite unitRepositoryWrite,
		ActionQueueRepository actionQueueRepository,
		GameTickEngine gameTickEngine,
		WorldState worldState,
		GameStateJsonSerializer serializer,
		IBlobStorage storage,
		IActionLogger actionLogger,
		GameDef gameDef,
		ScoreRepository scoreRepository,
		MarketRepository marketRepository
	)
	{
		this.logger = logger;
		this.options = options;
		this.playerRepository = playerRepository;
		this.playerRepositoryWrite = playerRepositoryWrite;
		this.resourceRepository = resourceRepository;
		this.resourceRepositoryWrite = resourceRepositoryWrite;
		this.assetRepositoryWrite = assetRepositoryWrite;
		this.unitRepositoryWrite = unitRepositoryWrite;
		this.actionQueueRepository = actionQueueRepository;
		this.gameTickEngine = gameTickEngine;
		this.worldState = worldState;
		this.serializer = serializer;
		this.storage = storage;
		this.actionLogger = actionLogger;
		this.gameDef = gameDef;
		this.scoreRepository = scoreRepository;
		this.marketRepository = marketRepository;
	}

	// ── Existing cheat endpoints ──────────────────────────────────────────────

	/// <summary>Dev-only: Sets a player's resource amount to the specified value. Requires Bge:DevAuth=true.</summary>
	[HttpPost("set-resources")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public ActionResult SetResources([FromBody] SetResourcesRequest request)
	{
		if (!options.Value.DevAuth) return NotFound();
		var playerId = PlayerIdFactory.Create(request.PlayerId);
		if (!playerRepository.Exists(playerId)) return NotFound($"Player '{request.PlayerId}' not found.");
		var resourceDefId = Id.ResDef(request.ResourceDefId);
		var current = resourceRepository.GetAmount(playerId, resourceDefId);
		resourceRepositoryWrite.AddResources(playerId, resourceDefId, request.Amount - current);
		return Ok();
	}

	/// <summary>Dev-only: Grants a building to a player instantly. Requires Bge:DevAuth=true.</summary>
	[HttpPost("grant-building")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public ActionResult GrantBuilding([FromBody] GrantBuildingRequest request)
	{
		if (!options.Value.DevAuth) return NotFound();
		var playerId = PlayerIdFactory.Create(request.PlayerId);
		if (!playerRepository.Exists(playerId)) return NotFound($"Player '{request.PlayerId}' not found.");
		var assetDefId = Id.AssetDef(request.AssetDefId);
		if (gameDef.GetAssetDef(assetDefId) == null) return BadRequest($"Unknown asset '{request.AssetDefId}'.");
		assetRepositoryWrite.GrantBuilding(playerId, assetDefId);
		return Ok();
	}

	/// <summary>Dev-only: Grants units to a player instantly. Requires Bge:DevAuth=true.</summary>
	[HttpPost("grant-units")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public ActionResult GrantUnits([FromBody] GrantUnitsRequest request)
	{
		if (!options.Value.DevAuth) return NotFound();
		var playerId = PlayerIdFactory.Create(request.PlayerId);
		if (!playerRepository.Exists(playerId)) return NotFound($"Player '{request.PlayerId}' not found.");
		var unitDefId = Id.UnitDef(request.UnitDefId);
		if (gameDef.GetUnitDef(unitDefId) == null) return BadRequest($"Unknown unit '{request.UnitDefId}'.");
		unitRepositoryWrite.GrantUnits(playerId, unitDefId, request.Count);
		return Ok();
	}

	/// <summary>Dev-only: Resets a player's state to initial values. Requires Bge:DevAuth=true.</summary>
	[HttpPost("reset-player")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public ActionResult ResetPlayer([FromBody] ResetPlayerRequest request)
	{
		if (!options.Value.DevAuth) return NotFound();
		var playerId = PlayerIdFactory.Create(request.PlayerId);
		if (!playerRepository.Exists(playerId)) return NotFound($"Player '{request.PlayerId}' not found.");
		actionQueueRepository.RemoveAllPlayerActions(playerId);
		playerRepositoryWrite.ResetPlayer(playerId);
		return Ok();
	}

	/// <summary>Dev-only: Forces N game ticks to run immediately. Requires Bge:DevAuth=true.</summary>
	/// <param name="count">Number of ticks to run (1–1000).</param>
	[HttpPost("force-tick")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public ActionResult ForceTick([FromQuery] int count = 1)
	{
		if (!options.Value.DevAuth) return NotFound();
		if (count < 1 || count > 1000) return BadRequest("count must be between 1 and 1000.");
		gameTickEngine.IncrementWorldTick(count);
		gameTickEngine.CheckAllTicks();
		return Ok();
	}

	[HttpGet("world-state")]
	public ActionResult WorldState()
	{
		if (!options.Value.DevAuth) return NotFound();
		var json = Encoding.UTF8.GetString(serializer.Serialize(worldState.ToImmutable()));
		return Content(json, "application/json");
	}

	[HttpGet("players")]
	public ActionResult<IEnumerable<string>> ListPlayers()
	{
		if (!options.Value.DevAuth) return NotFound();
		return Ok(playerRepository.GetAll().Select(p => p.PlayerId.Id));
	}

	// ── State Snapshots ───────────────────────────────────────────────────────

	[HttpPost("save-snapshot")]
	public async Task<ActionResult> SaveSnapshot([FromQuery] string name)
	{
		if (!options.Value.DevAuth) return NotFound();
		if (!IsValidSnapshotName(name)) return BadRequest("Invalid snapshot name. Use letters, digits, hyphens, and underscores only.");
		await storage.Store($"snapshots/{name}.json", serializer.Serialize(worldState.ToImmutable()));
		return Ok();
	}

	[HttpPost("load-snapshot")]
	public async Task<ActionResult> LoadSnapshot([FromQuery] string name)
	{
		if (!options.Value.DevAuth) return NotFound();
		if (!IsValidSnapshotName(name)) return BadRequest("Invalid snapshot name. Use letters, digits, hyphens, and underscores only.");
		var blobName = $"snapshots/{name}.json";
		if (!storage.Exists(blobName)) return NotFound($"Snapshot '{name}' not found.");
		var snapshot = serializer.Deserialize(await storage.Load(blobName));
		gameTickEngine.PauseTicks();
		try {
			worldState.ReplaceFrom(snapshot);
		} finally {
			gameTickEngine.ResumeTicks();
		}
		return Ok();
	}

	[HttpGet("snapshots")]
	public ActionResult<IEnumerable<string>> ListSnapshots()
	{
		if (!options.Value.DevAuth) return NotFound();
		var names = storage.List("snapshots")
			.Select(n => Path.GetFileNameWithoutExtension(n.Substring("snapshots/".Length)));
		return Ok(names);
	}

	[HttpDelete("snapshot")]
	public async Task<ActionResult> DeleteSnapshot([FromQuery] string name)
	{
		if (!options.Value.DevAuth) return NotFound();
		if (!IsValidSnapshotName(name)) return BadRequest("Invalid snapshot name. Use letters, digits, hyphens, and underscores only.");
		var blobName = $"snapshots/{name}.json";
		if (!storage.Exists(blobName)) return NotFound($"Snapshot '{name}' not found.");
		await storage.Delete(blobName);
		return Ok();
	}

	private static bool IsValidSnapshotName(string? name) =>
		!string.IsNullOrWhiteSpace(name) && name.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '_');

	// ── Tick Speed Control ────────────────────────────────────────────────────

	[HttpPost("set-tick-duration")]
	public ActionResult SetTickDuration([FromQuery] int seconds)
	{
		if (!options.Value.DevAuth) return NotFound();
		if (seconds < 1 || seconds > 86400) return BadRequest("seconds must be between 1 and 86400.");
		gameTickEngine.SetTickDuration(TimeSpan.FromSeconds(seconds));
		return Ok(new { seconds, message = $"Tick duration set to {seconds}s (was {gameDef.TickDuration.TotalSeconds}s default)." });
	}

	[HttpPost("pause-ticks")]
	public ActionResult PauseTicks()
	{
		if (!options.Value.DevAuth) return NotFound();
		gameTickEngine.PauseTicks();
		return Ok(new { paused = true });
	}

	[HttpPost("resume-ticks")]
	public ActionResult ResumeTicks()
	{
		if (!options.Value.DevAuth) return NotFound();
		gameTickEngine.ResumeTicks();
		return Ok(new { paused = false, effectiveTickDurationSeconds = gameTickEngine.EffectiveTickDuration.TotalSeconds });
	}

	// ── Action Feed ───────────────────────────────────────────────────────────

	[HttpGet("action-log")]
	public ActionResult<IEnumerable<ActionLogEntry>> GetActionLog()
	{
		if (!options.Value.DevAuth) return NotFound();
		return Ok(actionLogger.GetRecentEntries());
	}

	// ── Player Moderation ─────────────────────────────────────────────────────

	[HttpGet("players/{playerId}")]
	public ActionResult GetPlayerDetail(string playerId)
	{
		if (!options.Value.DevAuth) return NotFound();
		var pid = PlayerIdFactory.Create(playerId);
		if (!playerRepository.Exists(pid)) return NotFound($"Player '{playerId}' not found.");
		var player = playerRepository.Get(pid);
		var resources = new Dictionary<string, decimal>();
		foreach (var resDef in gameDef.Resources) {
			resources[resDef.Id.Id] = resourceRepository.GetAmount(pid, resDef.Id);
		}
		return Ok(new {
			playerId = player.PlayerId.Id,
			name = player.Name,
			playerType = player.PlayerType.Id,
			created = player.Created,
			lastOnline = player.LastOnline,
			isBanned = player.IsBanned,
			allianceId = player.AllianceId?.Id,
			score = scoreRepository.GetScore(pid),
			resources,
			unitCount = player.State.Units.Count,
			assetCount = player.State.Assets.Count,
			currentTick = player.State.CurrentGameTick.Tick,
			protectionTicksRemaining = player.State.ProtectionTicksRemaining,
			mineralWorkers = player.State.MineralWorkers,
			gasWorkers = player.State.GasWorkers,
		});
	}

	[HttpGet("players-detail")]
	public ActionResult GetPlayersDetail()
	{
		if (!options.Value.DevAuth) return NotFound();
		var players = playerRepository.GetAll().Select(p => new {
			playerId = p.PlayerId.Id,
			name = p.Name,
			playerType = p.PlayerType.Id,
			created = p.Created,
			lastOnline = p.LastOnline,
			isBanned = p.IsBanned,
			allianceId = p.AllianceId?.Id,
			score = scoreRepository.GetScore(p.PlayerId),
			unitCount = p.State.Units.Count,
			assetCount = p.State.Assets.Count,
		}).OrderByDescending(p => p.score);
		return Ok(players);
	}

	[HttpPost("ban-player")]
	public ActionResult BanPlayer([FromBody] BanPlayerRequest request)
	{
		if (!options.Value.DevAuth) return NotFound();
		var playerId = PlayerIdFactory.Create(request.PlayerId);
		if (!playerRepository.Exists(playerId)) return NotFound($"Player '{request.PlayerId}' not found.");
		playerRepositoryWrite.BanPlayer(playerId);
		logger.LogWarning("Player {PlayerId} banned via admin endpoint", request.PlayerId);
		return Ok(new { playerId = request.PlayerId, isBanned = true });
	}

	[HttpPost("unban-player")]
	public ActionResult UnbanPlayer([FromBody] UnbanPlayerRequest request)
	{
		if (!options.Value.DevAuth) return NotFound();
		var playerId = PlayerIdFactory.Create(request.PlayerId);
		if (!playerRepository.Exists(playerId)) return NotFound($"Player '{request.PlayerId}' not found.");
		playerRepositoryWrite.UnbanPlayer(playerId);
		logger.LogInformation("Player {PlayerId} unbanned via admin endpoint", request.PlayerId);
		return Ok(new { playerId = request.PlayerId, isBanned = false });
	}

	// ── Live Stats ────────────────────────────────────────────────────────────

	[HttpGet("live-stats")]
	public ActionResult GetLiveStats()
	{
		if (!options.Value.DevAuth) return NotFound();
		var players = playerRepository.GetAll().ToList();
		var totalResources = new Dictionary<string, decimal>();
		foreach (var resDef in gameDef.Resources) {
			totalResources[resDef.Id.Id] = players.Sum(p => p.State.Resources.TryGetValue(resDef.Id, out var val) ? val : 0);
		}
		var openOrders = marketRepository.GetOpenOrders();
		var recentlyOnline = players.Count(p => p.LastOnline.HasValue && p.LastOnline.Value > DateTime.UtcNow.AddMinutes(-15));
		var snapshot = worldState.ToImmutable();
		return Ok(new {
			totalPlayers = players.Count,
			activePlayers = recentlyOnline,
			bannedPlayers = players.Count(p => p.IsBanned),
			totalResources,
			openMarketOrders = openOrders.Count,
			currentTick = snapshot.GameTickState.CurrentGameTick.Tick,
			isPaused = gameTickEngine.IsPaused,
			effectiveTickDurationSeconds = gameTickEngine.EffectiveTickDuration.TotalSeconds,
		});
	}

	// ── Tick Status ───────────────────────────────────────────────────────────

	[HttpGet("tick-status")]
	public ActionResult GetTickStatus()
	{
		if (!options.Value.DevAuth) return NotFound();
		var snapshot = worldState.ToImmutable();
		return Ok(new {
			currentTick = snapshot.GameTickState.CurrentGameTick.Tick,
			isPaused = gameTickEngine.IsPaused,
			effectiveTickDurationSeconds = gameTickEngine.EffectiveTickDuration.TotalSeconds,
			defaultTickDurationSeconds = gameDef.TickDuration.TotalSeconds,
		});
	}
}

public record SetResourcesRequest(string PlayerId, string ResourceDefId, decimal Amount);
public record GrantBuildingRequest(string PlayerId, string AssetDefId);
public record GrantUnitsRequest(string PlayerId, string UnitDefId, int Count);
public record ResetPlayerRequest(string PlayerId);
public record BanPlayerRequest(string PlayerId);
public record UnbanPlayerRequest(string PlayerId);
