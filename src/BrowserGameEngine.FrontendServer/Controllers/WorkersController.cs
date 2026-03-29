using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.Shared;
using BrowserGameEngine.StatefulGameServer;
using BrowserGameEngine.StatefulGameServer.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace BrowserGameEngine.FrontendServer.Controllers {
	[ApiController]
	[Authorize]
	[Route("api/[controller]/{action?}")]
	public class WorkersController : ControllerBase {
		private readonly ILogger<WorkersController> logger;
		private readonly CurrentUserContext currentUserContext;
		private readonly PlayerRepository playerRepository;
		private readonly PlayerRepositoryWrite playerRepositoryWrite;
		private readonly UnitRepository unitRepository;
		private readonly GameDef gameDef;

		public WorkersController(ILogger<WorkersController> logger
				, CurrentUserContext currentUserContext
				, PlayerRepository playerRepository
				, PlayerRepositoryWrite playerRepositoryWrite
				, UnitRepository unitRepository
				, GameDef gameDef
			) {
			this.logger = logger;
			this.currentUserContext = currentUserContext;
			this.playerRepository = playerRepository;
			this.playerRepositoryWrite = playerRepositoryWrite;
			this.unitRepository = unitRepository;
			this.gameDef = gameDef;
		}

		[HttpGet]
		public ActionResult<WorkerAssignmentViewModel> Get() {
			if (!currentUserContext.IsValid) return Unauthorized();
			var playerId = currentUserContext.PlayerId!;
			var workerUnit = Id.UnitDef("wbf");
			int totalWorkers = unitRepository.CountByUnitDefId(playerId, workerUnit);
			return new WorkerAssignmentViewModel {
				TotalWorkers = totalWorkers,
				MineralWorkers = playerRepository.GetMineralWorkers(playerId),
				GasWorkers = playerRepository.GetGasWorkers(playerId)
			};
		}

		[HttpPost]
		public async Task<ActionResult> Assign([FromQuery] int mineralWorkers, [FromQuery] int gasWorkers) {
			if (!currentUserContext.IsValid) return Unauthorized();
			try {
				var playerId = currentUserContext.PlayerId!;
				var workerUnit = Id.UnitDef("wbf");
				int totalWorkers = unitRepository.CountByUnitDefId(playerId, workerUnit);
				playerRepositoryWrite.AssignWorkers(
					new AssignWorkersCommand(playerId, mineralWorkers, gasWorkers),
					totalWorkers
				);
				return Ok();
			} catch (ArgumentOutOfRangeException e) {
				return BadRequest(e.Message);
			}
		}
	}
}
