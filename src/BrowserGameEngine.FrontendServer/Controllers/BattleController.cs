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
using BrowserGameEngine.GameDefinition;
using System.Diagnostics;

namespace BrowserGameEngine.FrontendServer.Controllers {
	[ApiController]
	[Authorize]
	[Route("api/[controller]/{action?}")]
	public class BattleController : ControllerBase {
		private readonly ILogger<PlayerRankingController> logger;
		private readonly GameDef gameDef;
		private readonly CurrentUserContext currentUserContext;
		private readonly ScoreRepository scoreRepository;
		private readonly PlayerRepository playerRepository;
		private readonly UnitRepository unitRepository;
		private readonly UnitRepositoryWrite unitRepositoryWrite;

		public BattleController(ILogger<PlayerRankingController> logger
				, GameDef gameDef
				, CurrentUserContext currentUserContext
				, ScoreRepository scoreRepository
				, PlayerRepository playerRepository
				, UnitRepository unitRepository
				, UnitRepositoryWrite unitRepositoryWrite
			) {
			this.logger = logger;
			this.gameDef = gameDef;
			this.currentUserContext = currentUserContext;
			this.scoreRepository = scoreRepository;
			this.playerRepository = playerRepository;
			this.unitRepository = unitRepository;
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

		[HttpGet]
		public ActionResult<EnemyBaseViewModel> EnemyBase([FromQuery] string enemyPlayerId) {
			try {
				return new EnemyBaseViewModel {
					PlayerAttackingUnits = new UnitsViewModel {
						Units = unitRepository.GetAttackingUnits(currentUserContext.PlayerId, PlayerIdFactory.Create(enemyPlayerId))
							.Select(x => x.ToUnitViewModel(unitRepository, currentUserContext, gameDef)).ToList()
					},
					EnemyDefendingUnits = new UnitsViewModel {
						Units = unitRepository.GetDefendingEnemyUnits(currentUserContext.PlayerId, PlayerIdFactory.Create(enemyPlayerId))
							.Select(x => x.ToUnitViewModel(unitRepository, currentUserContext, gameDef)).ToList()
					}
				};
			} catch (CannotViewEnemyBaseException e) {
				return BadRequest(e.Message);
			}
		}


		[HttpPost]
		public BattleResultViewModel Attack([FromQuery] string enemyPlayerId) {
			var result = unitRepositoryWrite.Attack(currentUserContext.PlayerId, PlayerIdFactory.Create(enemyPlayerId));
			return new BattleResultViewModel {

			};
		}
	}
}
