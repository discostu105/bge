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

		/// <summary>Returns the current worker assignment: total workers and how many are assigned to minerals vs gas.</summary>
		[HttpGet]
		[ProducesResponseType(typeof(WorkerAssignmentViewModel), StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
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

		/// <summary>Reassigns workers between mineral and gas gathering. Total must not exceed available workers.</summary>
		/// <param name="mineralWorkers">Workers to assign to mineral harvesting.</param>
		/// <param name="gasWorkers">Workers to assign to gas harvesting.</param>
		[HttpPost]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
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
