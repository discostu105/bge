using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.Persistence;
using BrowserGameEngine.StatefulGameServer;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using BrowserGameEngine.StatefulGameServer.GameTicks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;

namespace BrowserGameEngine.FrontendServer.Controllers;

[ApiController]
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
	private readonly GameDef gameDef;

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
		GameDef gameDef
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
		this.gameDef = gameDef;
	}

	[HttpPost("set-resources")]
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

	[HttpPost("grant-building")]
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

	[HttpPost("grant-units")]
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

	[HttpPost("reset-player")]
	public ActionResult ResetPlayer([FromBody] ResetPlayerRequest request)
	{
		if (!options.Value.DevAuth) return NotFound();
		var playerId = PlayerIdFactory.Create(request.PlayerId);
		if (!playerRepository.Exists(playerId)) return NotFound($"Player '{request.PlayerId}' not found.");
		actionQueueRepository.RemoveAllPlayerActions(playerId);
		playerRepositoryWrite.ResetPlayer(playerId);
		return Ok();
	}

	[HttpPost("force-tick")]
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
}

public record SetResourcesRequest(string PlayerId, string ResourceDefId, decimal Amount);
public record GrantBuildingRequest(string PlayerId, string AssetDefId);
public record GrantUnitsRequest(string PlayerId, string UnitDefId, int Count);
public record ResetPlayerRequest(string PlayerId);
