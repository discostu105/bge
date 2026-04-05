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
		private readonly ILogger<BattleController> logger;
		private readonly GameDef gameDef;
		private readonly CurrentUserContext currentUserContext;
		private readonly ScoreRepository scoreRepository;
		private readonly PlayerRepository playerRepository;
		private readonly UserRepository userRepository;
		private readonly UnitRepository unitRepository;
		private readonly UnitRepositoryWrite unitRepositoryWrite;
		private readonly BattleReportGenerator battleReportGenerator;
		private readonly OnlineStatusRepository onlineStatusRepository;
		private readonly SpyRepositoryWrite spyRepositoryWrite;

		public BattleController(ILogger<BattleController> logger
				, GameDef gameDef
				, CurrentUserContext currentUserContext
				, ScoreRepository scoreRepository
				, PlayerRepository playerRepository
				, UserRepository userRepository
				, UnitRepository unitRepository
				, UnitRepositoryWrite unitRepositoryWrite
				, BattleReportGenerator battleReportGenerator
				, OnlineStatusRepository onlineStatusRepository
				, SpyRepositoryWrite spyRepositoryWrite
			) {
			this.logger = logger;
			this.gameDef = gameDef;
			this.currentUserContext = currentUserContext;
			this.scoreRepository = scoreRepository;
			this.playerRepository = playerRepository;
			this.userRepository = userRepository;
			this.unitRepository = unitRepository;
			this.unitRepositoryWrite = unitRepositoryWrite;
			this.battleReportGenerator = battleReportGenerator;
			this.onlineStatusRepository = onlineStatusRepository;
			this.spyRepositoryWrite = spyRepositoryWrite;
		}

		/// <summary>Lists players that the current player can attack.</summary>
		[HttpGet]
		[ProducesResponseType(typeof(SelectEnemyViewModel), StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		public ActionResult<SelectEnemyViewModel> AttackablePlayers() {
			if (!currentUserContext.IsValid) return Unauthorized();
			return new SelectEnemyViewModel {
				AttackablePlayers = playerRepository.GetAttackablePlayers(currentUserContext.PlayerId!).Select(p => p.ToPublicPlayerViewModel(scoreRepository, userRepository, onlineStatusRepository)).ToList()
			};
		}

		/// <summary>Sends a unit to attack an enemy player's base.</summary>
		/// <param name="unitId">The unit ID to send.</param>
		/// <param name="enemyPlayerId">The target enemy player ID.</param>
		[HttpPost]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		public ActionResult SendUnits([FromQuery] string unitId, [FromQuery] string enemyPlayerId) {
			if (!currentUserContext.IsValid) return Unauthorized();
			try {
				unitRepositoryWrite.SendUnit(new SendUnitCommand(currentUserContext.PlayerId!, Id.UnitId(unitId), PlayerIdFactory.Create(enemyPlayerId)));
				return Ok();
			} catch (PlayerNotAttackableException e) {
				return BadRequest(e.Message);
			} catch (UnitNotFoundException e) {
				return BadRequest(e.Message);
			} catch (UnitImmobileException e) {
				return BadRequest(e.Message);
			} catch (UnitNotHomeException e) {
				return BadRequest(e.Message);
			}
		}

		/// <summary>Returns information about an enemy player's base, including their defending units and your attacking units en route.</summary>
		/// <param name="enemyPlayerId">The enemy player ID to scout.</param>
		[HttpGet]
		[ProducesResponseType(typeof(EnemyBaseViewModel), StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		public ActionResult<EnemyBaseViewModel> EnemyBase([FromQuery] string enemyPlayerId) {
			if (!currentUserContext.IsValid) return Unauthorized();
			try {
				var spyCost = spyRepositoryWrite.GetSpyCost();
				var spyCostLabel = string.Join(", ", spyCost.ToPlainDictionary().Select(kv => $"{(int)kv.Value} {kv.Key}"));
				return new EnemyBaseViewModel {
					PlayerAttackingUnits = new UnitsViewModel {
						Units = unitRepository.GetAttackingUnits(currentUserContext.PlayerId!, PlayerIdFactory.Create(enemyPlayerId))
							.Select(x => x.ToUnitViewModel(unitRepository, currentUserContext, gameDef)).ToList()
					},
					EnemyDefendingUnits = new UnitsViewModel {
						Units = unitRepository.GetDefendingEnemyUnits(currentUserContext.PlayerId!, PlayerIdFactory.Create(enemyPlayerId))
							.Select(x => x.ToUnitViewModel(unitRepository, currentUserContext, gameDef)).ToList()
					},
					SpyCostLabel = spyCostLabel
				};
			} catch (CannotViewEnemyBaseException e) {
				return BadRequest(e.Message);
			}
		}


		/// <summary>Executes a battle against an enemy player using units already sent to their base.</summary>
		/// <param name="enemyPlayerId">The enemy player ID to attack.</param>
		[HttpPost]
		[ProducesResponseType(typeof(BattleResultViewModel), StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		public ActionResult<BattleResultViewModel> Attack([FromQuery] string enemyPlayerId) {
			if (!currentUserContext.IsValid) return Unauthorized();
			var result = unitRepositoryWrite.Attack(currentUserContext.PlayerId!, PlayerIdFactory.Create(enemyPlayerId));
			battleReportGenerator.GenerateReports(result);

			var attacker = playerRepository.Get(result.Attacker);
			var defender = playerRepository.Get(result.Defender);
			bool attackerWon = !result.BtlResult.DefendingUnitsSurvived.Any() && result.BtlResult.AttackingUnitsSurvived.Any();
			bool draw = !result.BtlResult.AttackingUnitsSurvived.Any() && !result.BtlResult.DefendingUnitsSurvived.Any();
			string outcome = attackerWon ? "AttackerWon" : draw ? "Draw" : "DefenderWon";

			logger.LogInformation(
				"Battle: attacker={AttackerName} defender={DefenderName} outcome={Outcome} attackerStrength={AttackerStrength} defenderStrength={DefenderStrength} landTransferred={LandTransferred}",
				attacker.Name, defender.Name, outcome,
				result.BtlResult.TotalAttackerStrengthBefore, result.BtlResult.TotalDefenderStrengthBefore,
				result.BtlResult.LandTransferred);

			return new BattleResultViewModel {
				AttackerId = result.Attacker.Id,
				AttackerName = attacker.Name,
				DefenderId = result.Defender.Id,
				DefenderName = defender.Name,
				Outcome = outcome,
				TotalAttackerStrengthBefore = result.BtlResult.TotalAttackerStrengthBefore,
				TotalDefenderStrengthBefore = result.BtlResult.TotalDefenderStrengthBefore,
				UnitsLostByAttacker = result.BtlResult.AttackingUnitsDestroyed
					.Select(uc => new UnitLossViewModel {
						UnitName = gameDef.GetUnitDef(uc.UnitDefId)?.Name ?? uc.UnitDefId.Id,
						Count = uc.Count
					}).ToList(),
				UnitsLostByDefender = result.BtlResult.DefendingUnitsDestroyed
					.Select(uc => new UnitLossViewModel {
						UnitName = gameDef.GetUnitDef(uc.UnitDefId)?.Name ?? uc.UnitDefId.Id,
						Count = uc.Count
					}).ToList(),
				ResourcesPillaged = result.BtlResult.ResourcesStolen
					.SelectMany(c => c.Resources)
					.GroupBy(x => x.Key.Id)
					.ToDictionary(g => g.Key, g => g.Sum(x => x.Value)),
				LandTransferred = (int)result.BtlResult.LandTransferred,
				WorkersCaptured = result.BtlResult.WorkersCaptured,
			};
		}
	}
}
