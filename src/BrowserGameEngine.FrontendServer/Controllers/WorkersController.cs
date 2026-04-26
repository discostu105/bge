using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.Shared;
using BrowserGameEngine.StatefulGameServer;
using BrowserGameEngine.StatefulGameServer.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;

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

		/// <summary>Returns the current worker auto-assignment: total workers, the gas percentage,
		/// and the resulting mineral/gas split.</summary>
		[HttpGet]
		[ProducesResponseType(typeof(WorkerAssignmentViewModel), StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		public ActionResult<WorkerAssignmentViewModel> Get() {
			if (!currentUserContext.IsValid) return Unauthorized();
			var playerId = currentUserContext.PlayerId!;
			// Sum across every configured worker unit so Zerg (drone) and Protoss (probe)
			// players see their workers, not just Terran (wbf).
			int totalWorkers = 0;
			foreach (var w in gameDef.GetWorkerUnitIds()) totalWorkers += unitRepository.CountByUnitDefId(playerId, w);
			int gasPercent = playerRepository.GetGasPercent(playerId);
			var (mineralWorkers, gasWorkers) = playerRepository.GetWorkerAssignment(playerId, totalWorkers);
			return new WorkerAssignmentViewModel {
				TotalWorkers = totalWorkers,
				GasPercent = gasPercent,
				MineralWorkers = mineralWorkers,
				GasWorkers = gasWorkers
			};
		}

		/// <summary>Sets the percentage of workers that auto-assign to gas (0–100). The rest go
		/// to minerals. The split is recomputed every tick from the current worker count.</summary>
		/// <param name="gasPercent">Percentage of workers gathering gas (0–100).</param>
		[HttpPost]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		public ActionResult Assign([FromQuery] int gasPercent) {
			if (!currentUserContext.IsValid) return Unauthorized();
			try {
				var playerId = currentUserContext.PlayerId!;
				playerRepositoryWrite.SetWorkerGasPercent(new SetWorkerGasPercentCommand(playerId, gasPercent));
				return Ok();
			} catch (ArgumentOutOfRangeException e) {
				return BadRequest(e.Message);
			}
		}
	}
}
