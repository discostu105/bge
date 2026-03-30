using BrowserGameEngine.GameModel;
using BrowserGameEngine.Shared;
using BrowserGameEngine.StatefulGameServer;
using BrowserGameEngine.StatefulGameServer.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BrowserGameEngine.FrontendServer.Controllers {
	[ApiController]
	[Authorize]
	[Route("api/[controller]/{action?}")]
	public class UpgradesController : ControllerBase {
		private readonly ILogger<UpgradesController> logger;
		private readonly CurrentUserContext currentUserContext;
		private readonly UpgradeRepository upgradeRepository;
		private readonly UpgradeRepositoryWrite upgradeRepositoryWrite;
		private readonly PlayerRepository playerRepository;

		public UpgradesController(
			ILogger<UpgradesController> logger,
			CurrentUserContext currentUserContext,
			UpgradeRepository upgradeRepository,
			UpgradeRepositoryWrite upgradeRepositoryWrite,
			PlayerRepository playerRepository) {
			this.logger = logger;
			this.currentUserContext = currentUserContext;
			this.upgradeRepository = upgradeRepository;
			this.upgradeRepositoryWrite = upgradeRepositoryWrite;
			this.playerRepository = playerRepository;
		}

		/// <summary>Returns the current player's attack and defense upgrade levels, plus costs for the next upgrade tier.</summary>
		[HttpGet]
		[ProducesResponseType(typeof(UpgradesViewModel), StatusCodes.Status200OK)]
		public UpgradesViewModel Get() {
			var playerId = currentUserContext.PlayerId!;
			int attackLevel = upgradeRepository.GetAttackUpgradeLevel(playerId);
			int defenseLevel = upgradeRepository.GetDefenseUpgradeLevel(playerId);
			return new UpgradesViewModel {
				AttackUpgradeLevel = attackLevel,
				DefenseUpgradeLevel = defenseLevel,
				UpgradeResearchTimer = upgradeRepository.GetUpgradeResearchTimer(playerId),
				UpgradeBeingResearched = upgradeRepository.GetUpgradeBeingResearched(playerId).ToString(),
				NextAttackUpgradeCost = attackLevel < 3 ? CostViewModel.Create(upgradeRepositoryWrite.GetUpgradeCost(attackLevel + 1)) : null,
				NextDefenseUpgradeCost = defenseLevel < 3 ? CostViewModel.Create(upgradeRepositoryWrite.GetUpgradeCost(defenseLevel + 1)) : null,
				PlayerType = playerRepository.GetPlayerType(playerId).Id
			};
		}

		/// <summary>Starts researching an upgrade. Only one upgrade can be researched at a time.</summary>
		/// <param name="upgradeType">Upgrade type: "Attack" or "Defense".</param>
		[HttpPost]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		public ActionResult Research([FromQuery] string upgradeType) {
			if (!System.Enum.TryParse<UpgradeType>(upgradeType, ignoreCase: true, out var type) || type == UpgradeType.None) {
				return BadRequest("Invalid upgradeType. Use 'Attack' or 'Defense'.");
			}
			try {
				upgradeRepositoryWrite.ResearchUpgrade(new ResearchUpgradeCommand(currentUserContext.PlayerId!, type));
				return Ok();
			} catch (UpgradeAlreadyMaxLevelException e) {
				return BadRequest(e.Message);
			} catch (UpgradeResearchInProgressException e) {
				return BadRequest(e.Message);
			} catch (CannotAffordException e) {
				return BadRequest(e.Message);
			}
		}
	}
}
