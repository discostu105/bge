using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.Shared;
using BrowserGameEngine.StatefulGameServer;
using BrowserGameEngine.StatefulGameServer.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;

namespace BrowserGameEngine.FrontendServer.Controllers {
	[ApiController]
	[Authorize]
	[Route("api/[controller]/{action?}")]
	public class ResearchController : ControllerBase {
		private readonly ILogger<ResearchController> logger;
		private readonly CurrentUserContext currentUserContext;
		private readonly GameDef gameDef;
		private readonly TechRepository techRepository;
		private readonly TechRepositoryWrite techRepositoryWrite;
		private readonly PlayerRepository playerRepository;

		public ResearchController(
			ILogger<ResearchController> logger,
			CurrentUserContext currentUserContext,
			GameDef gameDef,
			TechRepository techRepository,
			TechRepositoryWrite techRepositoryWrite,
			PlayerRepository playerRepository) {
			this.logger = logger;
			this.currentUserContext = currentUserContext;
			this.gameDef = gameDef;
			this.techRepository = techRepository;
			this.techRepositoryWrite = techRepositoryWrite;
			this.playerRepository = playerRepository;
		}

		/// <summary>Returns the current player's tech tree state.</summary>
		[HttpGet]
		[ProducesResponseType(typeof(TechTreeViewModel), StatusCodes.Status200OK)]
		public TechTreeViewModel Get() {
			var playerId = currentUserContext.PlayerId!;
			var currentResearch = techRepository.GetTechBeingResearched(playerId);
			var nodes = gameDef.TechNodes.Select(node => {
				string status;
				if (techRepository.IsUnlocked(playerId, node.Id)) {
					status = "Unlocked";
				} else if (currentResearch != null && currentResearch.Equals(node.Id)) {
					status = "InProgress";
				} else if (node.Prerequisites.All(p => techRepository.IsUnlocked(playerId, p)) && currentResearch == null) {
					status = "Available";
				} else {
					status = "Locked";
				}
				return new TechNodeViewModel {
					Id = node.Id.Id,
					Name = node.Name,
					Description = node.Description,
					Tier = node.Tier,
					Cost = CostViewModel.Create(node.Cost),
					ResearchTimeTicks = node.ResearchTimeTicks,
					PrerequisiteIds = node.Prerequisites.Select(p => p.Id).ToList(),
					EffectType = node.EffectType.ToString(),
					EffectValue = node.EffectValue,
					Status = status
				};
			}).ToList();

			return new TechTreeViewModel {
				PlayerType = playerRepository.GetPlayerType(playerId).Id,
				CurrentResearchId = currentResearch?.Id,
				ResearchTimerTicks = techRepository.GetResearchTimer(playerId),
				Nodes = nodes
			};
		}

		/// <summary>Start researching a tech node.</summary>
		/// <param name="techId">The ID of the tech node to research.</param>
		[HttpPost]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		public ActionResult Research([FromQuery] string techId) {
			if (!currentUserContext.IsValid) return Unauthorized();
			try {
				var command = new ResearchTechCommand(currentUserContext.PlayerId!, Id.TechNode(techId));
				techRepositoryWrite.StartResearch(command);
				return Ok();
			} catch (TechAlreadyUnlockedException e) {
				return BadRequest(e.Message);
			} catch (TechResearchInProgressException e) {
				return BadRequest(e.Message);
			} catch (TechPrerequisitesNotMetException e) {
				return BadRequest(e.Message);
			} catch (CannotAffordException e) {
				return BadRequest(e.Message);
			}
		}
	}
}
