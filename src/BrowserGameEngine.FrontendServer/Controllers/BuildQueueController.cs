using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.Shared;
using BrowserGameEngine.StatefulGameServer;
using BrowserGameEngine.StatefulGameServer.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BrowserGameEngine.FrontendServer.Controllers {
	[ApiController]
	[Authorize]
	[Route("api/[controller]/{action?}/{id?}")]
	public class BuildQueueController : ControllerBase {
		private readonly ILogger<BuildQueueController> logger;
		private readonly CurrentUserContext currentUserContext;
		private readonly BuildQueueRepository buildQueueRepository;
		private readonly BuildQueueRepositoryWrite buildQueueRepositoryWrite;
		private readonly GameDef gameDef;

		public BuildQueueController(ILogger<BuildQueueController> logger
				, CurrentUserContext currentUserContext
				, BuildQueueRepository buildQueueRepository
				, BuildQueueRepositoryWrite buildQueueRepositoryWrite
				, GameDef gameDef
			) {
			this.logger = logger;
			this.currentUserContext = currentUserContext;
			this.buildQueueRepository = buildQueueRepository;
			this.buildQueueRepositoryWrite = buildQueueRepositoryWrite;
			this.gameDef = gameDef;
		}

		[HttpGet]
		public BuildQueueViewModel Get() {
			if (!currentUserContext.IsValid) return new BuildQueueViewModel();
			var entries = buildQueueRepository.GetQueue(currentUserContext.PlayerId);
			return new BuildQueueViewModel {
				Entries = entries.Select(x => ToViewModel(x)).ToList()
			};
		}

		[HttpPost]
		public async Task<ActionResult> Add([FromBody] AddToQueueRequest request) {
			if (!currentUserContext.IsValid) return Unauthorized();
			if (request.Type != "unit" && request.Type != "asset") {
				return BadRequest("Invalid type. Must be 'unit' or 'asset'.");
			}
			if (request.Count <= 0) return BadRequest("Count must be greater than 0.");
			buildQueueRepositoryWrite.AddToQueue(new AddToQueueCommand(
				currentUserContext.PlayerId,
				request.Type,
				request.DefId,
				request.Count
			));
			return Ok();
		}

		[HttpDelete]
		public async Task<ActionResult> Remove([FromQuery] Guid entryId) {
			if (!currentUserContext.IsValid) return Unauthorized();
			buildQueueRepositoryWrite.RemoveFromQueue(new RemoveFromQueueCommand(currentUserContext.PlayerId, entryId));
			return Ok();
		}

		[HttpPost]
		public async Task<ActionResult> Reorder([FromBody] ReorderQueueRequest request) {
			if (!currentUserContext.IsValid) return Unauthorized();
			buildQueueRepositoryWrite.ReorderQueue(new ReorderQueueCommand(
				currentUserContext.PlayerId,
				request.EntryId,
				request.NewPriority
			));
			return Ok();
		}

		private BuildQueueEntryViewModel ToViewModel(BuildQueueEntryImmutable entry) {
			string name = entry.Type == "asset"
				? gameDef.GetAssetDef(Id.AssetDef(entry.DefId))?.Name ?? entry.DefId
				: gameDef.GetUnitDef(Id.UnitDef(entry.DefId))?.Name ?? entry.DefId;

			return new BuildQueueEntryViewModel {
				Id = entry.Id,
				Type = entry.Type,
				DefId = entry.DefId,
				Name = name,
				Count = entry.Count,
				Priority = entry.Priority
			};
		}
	}
}
