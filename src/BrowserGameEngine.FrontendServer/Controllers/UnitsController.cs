using BrowserGameEngine.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using BrowserGameEngine.StatefulGameServer;
using BrowserGameEngine.FrontendServer;
using BrowserGameEngine.StatefulGameServer.Commands;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.GameDefinition;
using Microsoft.AspNetCore.Authorization;

namespace BrowserGameEngine.FrontendServer.Controllers {
	[ApiController]
	[Authorize]
	[Route("api/[controller]/{action?}/{id?}")]
	public class UnitsController : ControllerBase {
		private readonly ILogger<UnitsController> logger;
		private readonly CurrentUserContext currentUserContext;
		private readonly PlayerRepository playerRepository;
		private readonly UnitRepository unitRepository;
		private readonly UnitRepositoryWrite unitRepositoryWrite;
		private readonly GameDef gameDef;

		public UnitsController(ILogger<UnitsController> logger
				, CurrentUserContext currentUserContext
				, PlayerRepository playerRepository
				, UnitRepository unitRepository
				, UnitRepositoryWrite unitRepositoryWrite
				, GameDef gameDef
			) {
			this.logger = logger;
			this.currentUserContext = currentUserContext;
			this.playerRepository = playerRepository;
			this.unitRepository = unitRepository;
			this.unitRepositoryWrite = unitRepositoryWrite;
			this.gameDef = gameDef;
		}

		[HttpGet]
		public UnitsViewModel Get() {
			return new UnitsViewModel {
				Units = unitRepository.GetAll(currentUserContext.PlayerId).Select(x => x.ToUnitViewModel(unitRepository, currentUserContext, gameDef)).ToList()
			};
		}

		[HttpPost]
		public async Task<ActionResult> Build([FromQuery] string unitDefId, [FromQuery] int count) {
			try {
				unitRepositoryWrite.BuildUnit(new BuildUnitCommand(currentUserContext.PlayerId, Id.UnitDef(unitDefId), count));
				return Ok();
			} catch (InvalidGameDefException e) {
				return BadRequest(e.Message);
			} catch (CannotAffordException e) {
				return BadRequest(e.Message);
			}
		}

		[HttpPost]
		public async Task<ActionResult> Merge([FromQuery] string? unitDefId) {
			try {
				if (string.IsNullOrEmpty(unitDefId)) {
					unitRepositoryWrite.MergeUnits(new MergeAllUnitsCommand(currentUserContext.PlayerId));
				} else {
					unitRepositoryWrite.MergeUnits(new MergeUnitsCommand(currentUserContext.PlayerId, Id.UnitDef(unitDefId)));
				}
				return Ok();
			} catch (InvalidGameDefException e) {
				return BadRequest(e.Message);
			}
		}

		[HttpPost]
		public async Task<ActionResult> Split([FromQuery] Guid unitId, [FromQuery] int splitCount) {
			try {
				unitRepositoryWrite.SplitUnit(new SplitUnitCommand(currentUserContext.PlayerId, Id.UnitId(unitId), splitCount));
				return Ok();
			} catch (InvalidGameDefException e) {
				return BadRequest(e.Message);
			}
		}
	}
}
