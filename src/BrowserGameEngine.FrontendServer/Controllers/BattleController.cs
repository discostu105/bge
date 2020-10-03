using BrowserGameEngine.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using BrowserGameEngine.StatefulGameServer;
using Microsoft.AspNetCore.Authorization;
using BrowserGameEngine.FrontendServer;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.Commands;

namespace BrowserGameEngine.FrontendServer.Controllers {
	[ApiController]
	[Authorize]
	[Route("api/[controller]/{action?}")]
	public class BattleController : ControllerBase {
		private readonly ILogger<PlayerRankingController> logger;
		private readonly CurrentUserContext currentUserContext;
		private readonly ScoreRepository scoreRepository;
		private readonly PlayerRepository playerRepository;
		private readonly UnitRepositoryWrite unitRepositoryWrite;

		public BattleController(ILogger<PlayerRankingController> logger
				, CurrentUserContext currentUserContext
				, ScoreRepository scoreRepository
				, PlayerRepository playerRepository
				, UnitRepositoryWrite unitRepositoryWrite
			) {
			this.logger = logger;
			this.currentUserContext = currentUserContext;
			this.scoreRepository = scoreRepository;
			this.playerRepository = playerRepository;
			this.unitRepositoryWrite = unitRepositoryWrite;
		}

		[HttpGet]
		public SelectEnemyViewModel AttackablePlayers() {
			return new SelectEnemyViewModel {
				AttackablePlayers = playerRepository.GetAttackablePlayers(currentUserContext.PlayerId).Select(p => p.ToPublicPlayerViewModel(scoreRepository)).ToList()
			};
		}

		[HttpPost]
		public async Task<ActionResult> SendUnits([FromQuery] string unitId, [FromQuery] string enemyPlayerId) {
			try {
				unitRepositoryWrite.SendUnit(new SendUnitCommand(currentUserContext.PlayerId, Id.UnitId(unitId), PlayerIdFactory.Create(enemyPlayerId)));
				return Ok();
			} catch (Exception e) {
				return BadRequest(e.Message);
			}
		}
	}
}
