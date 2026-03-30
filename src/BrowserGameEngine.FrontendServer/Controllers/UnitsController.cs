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
using System.Net.Http;

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

		/// <summary>Returns all units currently owned by the current player.</summary>
		[HttpGet]
		[ProducesResponseType(typeof(UnitsViewModel), StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		public ActionResult<UnitsViewModel> Get() {
			if (!currentUserContext.IsValid) return Unauthorized();
			return new UnitsViewModel {
				Units = unitRepository.GetAll(currentUserContext.PlayerId!).Select(x => x.ToUnitViewModel(unitRepository, currentUserContext, gameDef)).ToList()
			};
		}

		/// <summary>Trains a given number of units of the specified type.</summary>
		/// <param name="unitDefId">Unit definition ID (e.g. "marine").</param>
		/// <param name="count">Number of units to train.</param>
		[HttpPost]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		public async Task<ActionResult> Build([FromQuery] string unitDefId, [FromQuery] int count) {
			if (!currentUserContext.IsValid) return Unauthorized();
			try {
				unitRepositoryWrite.BuildUnit(new BuildUnitCommand(currentUserContext.PlayerId!, Id.UnitDef(unitDefId), count));
				return Ok();
			} catch (InvalidGameDefException e) {
				return BadRequest(e.Message);
			} catch (CannotAffordException e) {
				return BadRequest(e.Message);
			}
		}

		/// <summary>Merges multiple unit groups of the same type into one. If unitDefId is omitted, merges all unit types.</summary>
		/// <param name="unitDefId">Optional unit definition ID to merge only that type.</param>
		[HttpPost]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		public async Task<ActionResult> Merge([FromQuery] string? unitDefId) {
			if (!currentUserContext.IsValid) return Unauthorized();
			try {
				if (string.IsNullOrEmpty(unitDefId)) {
					unitRepositoryWrite.MergeUnits(new MergeAllUnitsCommand(currentUserContext.PlayerId!));
				} else {
					unitRepositoryWrite.MergeUnits(new MergeUnitsCommand(currentUserContext.PlayerId!, Id.UnitDef(unitDefId)));
				}
				return Ok();
			} catch (InvalidGameDefException e) {
				return BadRequest(e.Message);
			}
		}

		/// <summary>Splits a unit group into two groups.</summary>
		/// <param name="unitId">The unit group ID to split.</param>
		/// <param name="splitCount">Number of units to move to the new group.</param>
		[HttpPost]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		public async Task<ActionResult> Split([FromQuery] Guid unitId, [FromQuery] int splitCount) {
			if (!currentUserContext.IsValid) return Unauthorized();
			try {
				unitRepositoryWrite.SplitUnit(new SplitUnitCommand(currentUserContext.PlayerId!, Id.UnitId(unitId), splitCount));
				return Ok();
			} catch (InvalidGameDefException e) {
				return BadRequest(e.Message);
			}
		}
	}
}
