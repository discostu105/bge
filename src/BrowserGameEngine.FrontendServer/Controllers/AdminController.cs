using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.Persistence;
using BrowserGameEngine.StatefulGameServer;
using BrowserGameEngine.StatefulGameServer.ActionFeed;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using BrowserGameEngine.StatefulGameServer.GameRegistry;
using BrowserGameEngine.StatefulGameServer.GameTicks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrowserGameEngine.FrontendServer.Controllers;

[ApiController]
[Authorize(Policy = "Admin")]
[Route("api/admin")]
public class AdminController : ControllerBase
{
	private readonly ILogger<AdminController> logger;
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
	private readonly AdminAuditLog auditLog;
	private readonly ReportStore reportStore;
	private readonly GlobalState globalState;
	private readonly CurrentUserContext currentUserContext;

	public AdminController(
		ILogger<AdminController> logger,
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
		MarketRepository marketRepository,
		AdminAuditLog auditLog,
		ReportStore reportStore,
		GlobalState globalState,
		CurrentUserContext currentUserContext
	)
	{
		this.logger = logger;
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
		this.auditLog = auditLog;
		this.reportStore = reportStore;
		this.globalState = globalState;
		this.currentUserContext = currentUserContext;
	}

	// ── Existing cheat endpoints ──────────────────────────────────────────────

	/// <summary>Dev-only: Sets a player's resource amount to the specified value. Requires Bge:DevAuth=true.</summary>
	[HttpPost("set-resources")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public ActionResult SetResources([FromBody] SetResourcesRequest request)
	{

		var playerId = PlayerIdFactory.Create(request.PlayerId);
		if (!playerRepository.Exists(playerId)) return NotFound($"Player '{request.PlayerId}' not found.");
		var resourceDefId = Id.ResDef(request.ResourceDefId);
		var current = resourceRepository.GetAmount(playerId, resourceDefId);
		resourceRepositoryWrite.AddResources(playerId, resourceDefId, request.Amount - current);
		auditLog.Record("SetResources", currentUserContext.UserId,
			$"Set {request.ResourceDefId}={request.Amount} for player {request.PlayerId}", request.PlayerId);
		return Ok();
	}

	/// <summary>Dev-only: Grants a building to a player instantly. Requires Bge:DevAuth=true.</summary>
	[HttpPost("grant-building")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public ActionResult GrantBuilding([FromBody] GrantBuildingRequest request)
	{

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

		if (count < 1 || count > 1000) return BadRequest("count must be between 1 and 1000.");
		gameTickEngine.IncrementWorldTick(count);
		gameTickEngine.CheckAllTicks();
		return Ok();
	}

	[HttpGet("world-state")]
	public ActionResult WorldState()
	{

		var json = Encoding.UTF8.GetString(serializer.Serialize(worldState.ToImmutable()));
		return Content(json, "application/json");
	}

	[HttpGet("players")]
	public ActionResult<IEnumerable<string>> ListPlayers()
	{

		return Ok(playerRepository.GetAll().Select(p => p.PlayerId.Id));
	}

	// ── State Snapshots ───────────────────────────────────────────────────────

	[HttpPost("save-snapshot")]
	public async Task<ActionResult> SaveSnapshot([FromQuery] string name)
	{

		if (!IsValidSnapshotName(name)) return BadRequest("Invalid snapshot name. Use letters, digits, hyphens, and underscores only.");
		await storage.Store($"snapshots/{name}.json", serializer.Serialize(worldState.ToImmutable()));
		return Ok();
	}

	[HttpPost("load-snapshot")]
	public async Task<ActionResult> LoadSnapshot([FromQuery] string name)
	{

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

		var names = storage.List("snapshots")
			.Select(n => Path.GetFileNameWithoutExtension(n.Substring("snapshots/".Length)));
		return Ok(names);
	}

	[HttpDelete("snapshot")]
	public async Task<ActionResult> DeleteSnapshot([FromQuery] string name)
	{

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

		if (seconds < 1 || seconds > 86400) return BadRequest("seconds must be between 1 and 86400.");
		gameTickEngine.SetTickDuration(TimeSpan.FromSeconds(seconds));
		return Ok(new { seconds, message = $"Tick duration set to {seconds}s (was {gameDef.TickDuration.TotalSeconds}s default)." });
	}

	[HttpPost("pause-ticks")]
	public ActionResult PauseTicks()
	{

		gameTickEngine.PauseTicks();
		return Ok(new { paused = true });
	}

	[HttpPost("resume-ticks")]
	public ActionResult ResumeTicks()
	{

		gameTickEngine.ResumeTicks();
		return Ok(new { paused = false, effectiveTickDurationSeconds = gameTickEngine.EffectiveTickDuration.TotalSeconds });
	}

	// ── Action Feed ───────────────────────────────────────────────────────────

	[HttpGet("action-log")]
	public ActionResult<IEnumerable<ActionLogEntry>> GetActionLog()
	{

		return Ok(actionLogger.GetRecentEntries());
	}

	// ── Player Moderation ─────────────────────────────────────────────────────

	[HttpGet("players/{playerId}")]
	public ActionResult GetPlayerDetail(string playerId)
	{

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

		var playerId = PlayerIdFactory.Create(request.PlayerId);
		if (!playerRepository.Exists(playerId)) return NotFound($"Player '{request.PlayerId}' not found.");
		playerRepositoryWrite.BanPlayer(playerId);
		logger.LogWarning("Player {PlayerId} banned via admin endpoint", request.PlayerId);
		auditLog.Record("BanPlayer", currentUserContext.UserId, $"Banned player {request.PlayerId}", request.PlayerId);
		return Ok(new { playerId = request.PlayerId, isBanned = true });
	}

	[HttpPost("unban-player")]
	public ActionResult UnbanPlayer([FromBody] UnbanPlayerRequest request)
	{

		var playerId = PlayerIdFactory.Create(request.PlayerId);
		if (!playerRepository.Exists(playerId)) return NotFound($"Player '{request.PlayerId}' not found.");
		playerRepositoryWrite.UnbanPlayer(playerId);
		logger.LogInformation("Player {PlayerId} unbanned via admin endpoint", request.PlayerId);
		auditLog.Record("UnbanPlayer", currentUserContext.UserId, $"Unbanned player {request.PlayerId}", request.PlayerId);
		return Ok(new { playerId = request.PlayerId, isBanned = false });
	}

	// ── Live Stats ────────────────────────────────────────────────────────────

	[HttpGet("live-stats")]
	public ActionResult GetLiveStats()
	{

		var players = playerRepository.GetAll().ToList();
		var totalResources = new Dictionary<string, decimal>();
		foreach (var resDef in gameDef.Resources) {
			totalResources[resDef.Id.Id] = players.Sum(p => p.State.Resources.TryGetValue(resDef.Id, out var val) ? val : 0);
		}
		var openOrders = marketRepository.GetOpenOrders();
		var recentlyOnline = players.Count(p => p.LastOnline.HasValue && p.LastOnline.Value > DateTime.UtcNow.AddMinutes(-15));
		var snapshot = worldState.ToImmutable();

		// Extended metrics
		var now = DateTime.UtcNow;
		var dayAgo = now.AddDays(-1);
		var allGames = globalState.GetGames().ToList();
		var runningGames = allGames.Count(g => g.Status == GameStatus.Active);

		// Daily active users: unique user IDs who have a player active in the last 24h
		var dailyActiveUsers = players
			.Where(p => p.UserId != null && p.LastOnline.HasValue && p.LastOnline.Value >= dayAgo)
			.Select(p => p.UserId!)
			.Distinct()
			.Count();

		return Ok(new {
			totalPlayers = players.Count,
			activePlayers = recentlyOnline,
			bannedPlayers = players.Count(p => p.IsBanned),
			totalResources,
			openMarketOrders = openOrders.Count,
			currentTick = snapshot.GameTickState.CurrentGameTick.Tick,
			isPaused = gameTickEngine.IsPaused,
			effectiveTickDurationSeconds = gameTickEngine.EffectiveTickDuration.TotalSeconds,
			dailyActiveUsers,
			runningGames,
			totalGames = allGames.Count,
		});
	}

	// ── Tick Status ───────────────────────────────────────────────────────────

	[HttpGet("tick-status")]
	public ActionResult GetTickStatus()
	{

		var snapshot = worldState.ToImmutable();
		return Ok(new {
			currentTick = snapshot.GameTickState.CurrentGameTick.Tick,
			isPaused = gameTickEngine.IsPaused,
			effectiveTickDurationSeconds = gameTickEngine.EffectiveTickDuration.TotalSeconds,
			defaultTickDurationSeconds = gameDef.TickDuration.TotalSeconds,
		});
	}

	// ── Audit Log ─────────────────────────────────────────────────────────────

	[HttpGet("audit-log")]
	public ActionResult GetAuditLog([FromQuery] int page = 0, [FromQuery] int pageSize = 20, [FromQuery] string actionType = "all")
	{
		if (pageSize < 1 || pageSize > 100) return BadRequest("pageSize must be between 1 and 100.");
		var entries = auditLog.GetPage(page, pageSize, actionType);
		var total = auditLog.GetTotal(actionType);
		return Ok(new { entries, total, page, pageSize });
	}

	// ── Reports ───────────────────────────────────────────────────────────────

	[HttpGet("reports")]
	public ActionResult GetReports()
	{
		var reports = reportStore.GetAll().Select(r => new {
			id = r.Id,
			createdAt = r.CreatedAt,
			reporterUserId = r.ReporterUserId,
			targetUserId = r.TargetUserId,
			reason = r.Reason,
			details = r.Details,
			status = r.Status.ToString(),
			resolvedByUserId = r.ResolvedByUserId,
			resolutionNote = r.ResolutionNote,
			resolvedAt = r.ResolvedAt,
		});
		return Ok(reports);
	}

	[HttpPost("reports/{id}/action")]
	public ActionResult ResolveReport(string id, [FromBody] ReportActionRequest request)
	{
		var report = reportStore.GetById(id);
		if (report == null) return NotFound();
		if (!Enum.TryParse<ReportStatus>(request.Action, true, out var newStatus))
			return BadRequest($"Invalid action. Valid values: Resolved, Dismissed.");
		var updated = report with {
			Status = newStatus,
			ResolvedByUserId = currentUserContext.UserId,
			ResolutionNote = request.Note,
			ResolvedAt = DateTime.UtcNow,
		};
		reportStore.Update(report, updated);
		auditLog.Record("ReportAction", currentUserContext.UserId,
			$"Report {id}: {newStatus} — {request.Note}", report.TargetUserId);
		return Ok();
	}
}

public record SetResourcesRequest(string PlayerId, string ResourceDefId, decimal Amount);
public record GrantBuildingRequest(string PlayerId, string AssetDefId);
public record GrantUnitsRequest(string PlayerId, string UnitDefId, int Count);
public record ResetPlayerRequest(string PlayerId);
public record BanPlayerRequest(string PlayerId);
public record UnbanPlayerRequest(string PlayerId);
public record ReportActionRequest(string Action, string? Note);
